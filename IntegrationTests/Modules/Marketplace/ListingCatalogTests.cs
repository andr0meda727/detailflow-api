using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Marketplace.Infrastructure.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Scheduling.Contracts;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Vehicles;

namespace IntegrationTests.Modules.Marketplace;

/// <summary>
/// The listing's copy of the studio's services follows Scheduling through the outbox and the bus.
/// </summary>
[TestFixture]
public class ListingCatalogTests
{
    private ApiFactory _factory = null!;
    private TwoStudiosSeed _seed = null!;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
        _factory = new ApiFactory();

        foreach (var studio in new[] { _seed.StudioA, _seed.StudioB })
        {
            using var owner = _factory.CreateClientFor(studio.OwnerUserId);
            var saved = await owner.PutAsJsonAsync("/api/marketplace/listing", new
            {
                name = "Studio", address = "ul. Długa 1", city = "Kraków", latitude = 50.06, longitude = 19.94
            });
            saved.EnsureSuccessStatusCode();
        }
    }

    [OneTimeTearDown]
    public async Task TearDownAsync() => await _factory.DisposeAsync();

    [Test]
    public async Task ChangingAServiceInScheduling_UpdatesTheListing()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var update = await owner.PutAsJsonAsync($"/api/service/{_seed.StudioA.ServiceId}", new
        {
            name = "Pranie tapicerki", category = "Interior", prices = new[] { new { vehicleSize = "Large", price = 350m } }
        });
        update.EnsureSuccessStatusCode();

        // The event goes through the outbox and the bus, so it arrives a moment later
        JsonElement service = default;
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var listing = await owner.GetFromJsonAsync<JsonElement>("/api/marketplace/listing");
            service = listing.GetProperty("services")[0];
            if (service.GetProperty("category").GetString() == "Interior") break;
            await Task.Delay(100);
        }

        Assert.That(service.GetProperty("name").GetString(), Is.EqualTo("Pranie tapicerki"));
        Assert.That(service.GetProperty("category").GetString(), Is.EqualTo("Interior"));
        Assert.That(service.GetProperty("prices")[0].GetProperty("price").GetDecimal(), Is.EqualTo(350m));
    }

    [Test]
    public async Task AnOlderSnapshot_DoesNotUndoANewerOne()
    {
        var studioId = _seed.StudioB.StudioId;
        var serviceId = _seed.StudioB.ServiceId;
        var now = DateTime.UtcNow.AddMinutes(5);

        await ConsumeAsync(new ServiceCatalogChanged(studioId, now,
            [new CatalogService(serviceId, "Nowa nazwa", "Polishing", [new CatalogPrice("Small", 500)])]));
        // Same size again, new amount: the owned price row is replaced, not duplicated
        await ConsumeAsync(new ServiceCatalogChanged(studioId, now.AddSeconds(1),
            [new CatalogService(serviceId, "Nowa nazwa", "Polishing", [new CatalogPrice("Small", 600)])]));
        await ConsumeAsync(new ServiceCatalogChanged(studioId, now.AddSeconds(-30),
            [new CatalogService(serviceId, "Stara nazwa", "Other", [])]));

        await using var db = PostgresFixture.CreateMarketplaceDbContext(new TenantContext());
        var listing = await db.Listings.AsNoTracking().SingleAsync(l => l.StudioId == studioId);
        Assert.That(listing.Services.Single().Name, Is.EqualTo("Nowa nazwa"));
        Assert.That(listing.Prices.Single().Size, Is.EqualTo(VehicleSize.Small));
        Assert.That(listing.Prices.Single().AmountInCents, Is.EqualTo(600));
    }

    private static async Task ConsumeAsync(ServiceCatalogChanged message)
    {
        await using var db = PostgresFixture.CreateMarketplaceDbContext(new TenantContext());
        var context = Substitute.For<ConsumeContext<ServiceCatalogChanged>>();
        context.Message.Returns(message);

        await new ListingCatalogConsumer(db).Consume(context);
    }
}
