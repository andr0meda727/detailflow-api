using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inventory.Application.Consumption;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Scheduling.Application.Bookings;
using Scheduling.Domain.Bookings;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Vehicles;

namespace IntegrationTests.Modules.Inventory;

/// <summary>
/// Materials end to end: a service's recipe, a visit completed in Scheduling, the BookingCompleted event over the bus,
/// stock taken off in Inventory, then staff correcting it and the forecast. Each test makes its own service and
/// material, so their stock doesn't depend on the other tests.
/// </summary>
[TestFixture]
public class ConsumptionEndpointTests
{
    private ApiFactory _factory = null!;
    private TwoStudiosSeed _seed = null!;
    private HttpClient _owner = null!;
    private Guid _largeCarId;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
        _factory = new ApiFactory();
        _owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        _largeCarId = await IdOfAsync(await _owner.PostAsJsonAsync("/api/vehicle",
            new { clientId = _seed.StudioA.ClientId, licensePlate = "KR LARGE", size = "Large" }));
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        _owner.Dispose();
        await _factory.DisposeAsync();
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var code) ? code.GetString() : null;

    // A new service using `perMedium` of a new material that starts with `stock`
    private async Task<(Guid ServiceId, Guid ItemId)> ServiceUsingAsync(decimal perMedium, decimal stock)
    {
        var serviceId = await IdOfAsync(await _owner.PostAsJsonAsync("/api/service", new { name = $"Usługa {Guid.NewGuid():N}"[..20] }));
        var itemId = await IdOfAsync(await _owner.PostAsJsonAsync("/api/inventory", new { name = $"Szampon {Guid.NewGuid():N}"[..20], unit = "l", stock }));
        (await _owner.PutAsJsonAsync($"/api/inventory/recipes/{serviceId}", new { lines = new[] { new { itemId, quantityForMedium = perMedium } } }))
            .EnsureSuccessStatusCode();
        return (serviceId, itemId);
    }

    private async Task<Guid> BookAsync(Guid serviceId, Guid vehicleId, DateTime at) =>
        await IdOfAsync(await _owner.PostAsJsonAsync("/api/bookings", new BookingRequest
        {
            ClientId = _seed.StudioA.ClientId, VehicleId = vehicleId, StatusId = BookingStatus.Created,
            ScheduledAt = at, ServiceIds = [serviceId], Price = 100m
        }));

    private async Task<Guid> CompleteVisitAsync(Guid serviceId, Guid vehicleId)
    {
        var bookingId = await BookAsync(serviceId, vehicleId, DateTime.UtcNow.AddHours(1));
        (await _owner.PostAsync($"/api/bookings/{bookingId}/complete", null)).EnsureSuccessStatusCode();
        return bookingId;
    }

    // The consumer runs on the bus, after the request
    private async Task<JsonElement> WaitForConsumptionAsync(Guid bookingId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var response = await _owner.GetAsync($"/api/inventory/consumptions/{bookingId}");
            if (response.StatusCode == HttpStatusCode.OK) return await response.Content.ReadFromJsonAsync<JsonElement>();
            if (DateTime.UtcNow > deadline) Assert.Fail($"No consumption for booking {bookingId}: {response.StatusCode}");
            await Task.Delay(100);
        }
    }

    private async Task<decimal> StockOfAsync(Guid itemId) =>
        (await _owner.GetFromJsonAsync<JsonElement>($"/api/inventory/{itemId}")).GetProperty("stock").GetDecimal();

    [Test]
    public async Task CompletingAVisit_TakesTheSizeScaledEstimateOffStock()
    {
        var (service, item) = await ServiceUsingAsync(perMedium: 0.2m, stock: 10);

        var bookingId = await CompleteVisitAsync(service, _largeCarId);
        var consumption = await WaitForConsumptionAsync(bookingId);

        // A Large car takes 1.3 times the Medium norm
        var line = consumption.GetProperty("lines").EnumerateArray().Single();
        Assert.That(consumption.GetProperty("vehicleSize").GetString(), Is.EqualTo("Large"));
        Assert.That(line.GetProperty("estimatedQuantity").GetDecimal(), Is.EqualTo(0.26m));
        Assert.That(await StockOfAsync(item), Is.EqualTo(9.74m));

        var history = await _owner.GetFromJsonAsync<JsonElement>($"/api/inventory/{item}/movements");
        Assert.That(history.GetProperty("items").EnumerateArray().Select(m => m.GetProperty("type").GetString()),
            Is.EqualTo(new[] { "Consumption", "Initial" }));
    }

    [Test]
    public async Task CorrectingAVisit_MovesStockByTheDifference_AndLearnsFromItOnce()
    {
        var (service, item) = await ServiceUsingAsync(perMedium: 0.2m, stock: 10);
        var bookingId = await CompleteVisitAsync(service, _seed.StudioA.VehicleId);
        await WaitForConsumptionAsync(bookingId);

        var first = await _owner.PutAsJsonAsync($"/api/inventory/consumptions/{bookingId}",
            new { lines = new[] { new { serviceId = service, itemId = item, actualQuantity = 0.3m } } });
        var stockAfterFirst = await StockOfAsync(item);
        var second = await _owner.PutAsJsonAsync($"/api/inventory/consumptions/{bookingId}",
            new { lines = new[] { new { serviceId = service, itemId = item, actualQuantity = 0.25m } } });

        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(stockAfterFirst, Is.EqualTo(9.7m), "10 - 0.2 estimated - 0.1 more than estimated");
        Assert.That(await StockOfAsync(item), Is.EqualTo(9.75m));

        var accuracy = (await _owner.GetFromJsonAsync<JsonElement>("/api/inventory/consumptions/accuracy"))
            .EnumerateArray().Single(a => a.GetProperty("itemId").GetGuid() == item);
        Assert.That(accuracy.GetProperty("samples").GetInt32(), Is.EqualTo(1), "a visit is learned from once");
        Assert.That(accuracy.GetProperty("factorApplied").GetBoolean(), Is.False);
    }

    [Test]
    public async Task TheSameCompletion_TwiceIsRecordedOnce()
    {
        var (service, item) = await ServiceUsingAsync(perMedium: 0.5m, stock: 5);
        var bookingId = Guid.NewGuid();

        // As the consumer does it: no tenant, the studio from the event
        async Task<bool> RecordAsync()
        {
            await using var db = PostgresFixture.CreateInventoryDbContext(new TenantContext());
            return await new ConsumptionRecorder(db).RecordAsync(
                _seed.StudioA.StudioId, bookingId, VehicleSize.Medium, [service], DateTime.UtcNow, CancellationToken.None);
        }

        Assert.That(await RecordAsync(), Is.True);
        Assert.That(await RecordAsync(), Is.False);
        Assert.That(await StockOfAsync(item), Is.EqualTo(4.5m));
    }

    [Test]
    public async Task Forecast_SaysWhichBookedVisitStockWontCover()
    {
        var (service, item) = await ServiceUsingAsync(perMedium: 0.2m, stock: 0.3m);
        var firstVisit = DateTime.UtcNow.Date.AddDays(2).AddHours(9);
        await BookAsync(service, _seed.StudioA.VehicleId, firstVisit);
        await BookAsync(service, _seed.StudioA.VehicleId, firstVisit.AddDays(1));

        var forecast = (await _owner.GetFromJsonAsync<JsonElement>("/api/inventory/forecast"))
            .EnumerateArray().Single(f => f.GetProperty("itemId").GetGuid() == item);

        Assert.That(forecast.GetProperty("plannedUse").GetDecimal(), Is.EqualTo(0.4m));
        Assert.That(forecast.GetProperty("stockAfterPlanned").GetDecimal(), Is.EqualTo(-0.1m));
        Assert.That(forecast.GetProperty("runsOutAt").GetDateTime(), Is.EqualTo(firstVisit.AddDays(1)));
    }

    [Test]
    public async Task Recipe_OnlyUsesThisStudiosServicesAndMaterials()
    {
        var unknownService = await _owner.PutAsJsonAsync($"/api/inventory/recipes/{Guid.NewGuid()}",
            new { lines = Array.Empty<object>() });
        var othersMaterial = await _owner.PutAsJsonAsync($"/api/inventory/recipes/{_seed.StudioA.ServiceId}",
            new { lines = new[] { new { itemId = _seed.StudioB.InventoryId, quantityForMedium = 0.1m } } });

        Assert.That(await ProblemCodeAsync(unknownService), Is.EqualTo("SERVICE_NOT_FOUND"));
        Assert.That(othersMaterial.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Stock_ChangesThroughDeliveriesAndStocktakes()
    {
        var (_, item) = await ServiceUsingAsync(perMedium: 0.1m, stock: 2);

        var delivery = await _owner.PostAsJsonAsync($"/api/inventory/{item}/movements", new { kind = "Purchase", quantity = 3m, note = "Faktura 12/11" });
        var count = await _owner.PostAsJsonAsync($"/api/inventory/{item}/movements", new { kind = "Stocktake", quantity = 4.5m });
        var empty = await _owner.PostAsJsonAsync($"/api/inventory/{item}/movements", new { kind = "Purchase", quantity = 0m });

        Assert.That((await delivery.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stock").GetDecimal(), Is.EqualTo(5m));
        Assert.That((await count.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stock").GetDecimal(), Is.EqualTo(4.5m));
        Assert.That(empty.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
