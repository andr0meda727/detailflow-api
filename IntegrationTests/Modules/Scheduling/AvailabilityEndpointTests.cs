using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Scheduling.Application.Bookings;
using Scheduling.Domain.Bookings;
using Shared.Kernel.Time;

namespace IntegrationTests.Modules.Scheduling;

/// <summary>
/// Free times for staff and customers, and the day board. Studio A plans (two bays, open 08:00-16:00 every day, a
/// two-hour service) and is published; studio B doesn't plan and isn't published.
/// </summary>
[TestFixture]
public class AvailabilityEndpointTests
{
    private static readonly TimeZoneInfo Warsaw = TimeZones.Find("Europe/Warsaw");

    private ApiFactory _factory = null!;
    private TwoStudiosSeed _seed = null!;
    private Guid _bay1;
    private Guid _bay2;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
        _factory = new ApiFactory();

        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        (await owner.PutAsJsonAsync($"/api/service/{_seed.StudioA.ServiceId}", new { name = "Polerowanie", durationMinutes = 120 }))
            .EnsureSuccessStatusCode();
        _bay1 = await IdOfAsync(await owner.PostAsJsonAsync("/api/bays", new { name = "A" }));
        _bay2 = await IdOfAsync(await owner.PostAsJsonAsync("/api/bays", new { name = "B" }));
        (await owner.PutAsJsonAsync("/api/schedule", new
        {
            openingHours = Enum.GetValues<DayOfWeek>().Select(day => new { day = day.ToString(), opens = "08:00:00", closes = "16:00:00" }),
            bufferMinutes = 15, minLeadHours = 12
        })).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync("/api/marketplace/listing", new
        {
            name = "Detail Studio", address = "ul. Długa 1", city = "Kraków", latitude = 50.06, longitude = 19.94
        })).EnsureSuccessStatusCode();
        (await owner.PostAsync("/api/marketplace/listing/publish", null)).EnsureSuccessStatusCode();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync() => await _factory.DisposeAsync();

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static DateOnly DayAhead(int days) => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(days));

    private static DateTime LocalAt(DateOnly day, int hour) => TimeZones.ToUtc(day.ToDateTime(new TimeOnly(hour, 0)), Warsaw);

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var code) ? code.GetString() : null;

    private async Task BookAsync(DateTime at, Guid bayId)
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        (await owner.PostAsJsonAsync("/api/bookings", new BookingRequest
        {
            ClientId = _seed.StudioA.ClientId, VehicleId = _seed.StudioA.VehicleId, StatusId = BookingStatus.Created,
            ScheduledAt = at, ServiceIds = [_seed.StudioA.ServiceId], Price = 300m, BayId = bayId
        })).EnsureSuccessStatusCode();
    }

    [Test]
    public async Task Staff_SeeFreeTimes_WithTheSuggestedBay_AndHowManyAreFree()
    {
        var day = DayAhead(3);
        await BookAsync(LocalAt(day, 8), _bay1);
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var result = await owner.GetFromJsonAsync<JsonElement>(
            $"/api/availability?from={day:yyyy-MM-dd}&serviceIds={_seed.StudioA.ServiceId}");

        Assert.That(result.GetProperty("planningEnabled").GetBoolean(), Is.True);
        Assert.That(result.GetProperty("durationMinutes").GetInt32(), Is.EqualTo(120));
        var slots = result.GetProperty("slots").EnumerateArray().ToList();
        var eight = slots.Single(s => s.GetProperty("start").GetDateTime() == LocalAt(day, 8));
        Assert.That(eight.GetProperty("freeBays").GetInt32(), Is.EqualTo(1));
        Assert.That(eight.GetProperty("suggestedBayId").GetGuid(), Is.EqualTo(_bay2));
        Assert.That(slots.Last().GetProperty("end").GetDateTime(), Is.EqualTo(LocalAt(day, 16)));
    }

    [Test]
    public async Task ALargerCar_TakesLonger()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var result = await owner.GetFromJsonAsync<JsonElement>(
            $"/api/availability?from={DayAhead(4):yyyy-MM-dd}&serviceIds={_seed.StudioA.ServiceId}&vehicleSize=XL");

        Assert.That(result.GetProperty("durationMinutes").GetInt32(), Is.EqualTo(180));
    }

    [Test]
    public async Task AStudioThatDoesNotPlan_SaysSo()
    {
        using var other = _factory.CreateClientFor(_seed.StudioB.OwnerUserId);

        var result = await other.GetFromJsonAsync<JsonElement>(
            $"/api/availability?from={DayAhead(3):yyyy-MM-dd}&serviceIds={_seed.StudioB.ServiceId}");

        Assert.That(result.GetProperty("planningEnabled").GetBoolean(), Is.False);
        Assert.That(result.GetProperty("slots").GetArrayLength(), Is.Zero);
    }

    [Test]
    public async Task AnotherStudiosService_OrTooLongARange_IsRejected()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var foreign = await owner.GetAsync($"/api/availability?from={DayAhead(3):yyyy-MM-dd}&serviceIds={_seed.StudioB.ServiceId}");
        var tooLong = await owner.GetAsync(
            $"/api/availability?from={DayAhead(3):yyyy-MM-dd}&to={DayAhead(30):yyyy-MM-dd}&serviceIds={_seed.StudioA.ServiceId}");

        Assert.That(await ProblemCodeAsync(foreign), Is.EqualTo("SERVICE_NOT_FOUND"));
        Assert.That(tooLong.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Customers_SeeOnlyStartTimes_OfAPublishedStudio_AfterTheLeadTime()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync(
            $"/api/public/studios/{_seed.StudioA.StudioId}/availability?from={DayAhead(0):yyyy-MM-dd}&to={DayAhead(2):yyyy-MM-dd}&serviceIds={_seed.StudioA.ServiceId}");
        var unpublished = await anonymous.GetAsync(
            $"/api/public/studios/{_seed.StudioB.StudioId}/availability?from={DayAhead(1):yyyy-MM-dd}&serviceIds={_seed.StudioB.ServiceId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var slots = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("slots").EnumerateArray().ToList();
        Assert.That(slots, Is.Not.Empty);
        Assert.That(slots.All(s => s.GetProperty("start").GetDateTime() >= DateTime.UtcNow.AddHours(12).AddMinutes(-1)), Is.True);
        Assert.That(slots[0].TryGetProperty("suggestedBayId", out _), Is.False, "customers don't see bays");
        Assert.That(await ProblemCodeAsync(unpublished), Is.EqualTo("STUDIO_NOT_FOUND"));
    }

    [Test]
    public async Task DayBoard_ShowsEachBaysBookingsGapsAndHowBusyItIs()
    {
        var day = DayAhead(5);
        await BookAsync(LocalAt(day, 10), _bay1);
        await BookAsync(LocalAt(day, 13), _bay1);
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var board = await owner.GetFromJsonAsync<JsonElement>($"/api/schedule/day?date={day:yyyy-MM-dd}");

        Assert.That(board.GetProperty("openMinutes").GetInt32(), Is.EqualTo(480));
        var bays = board.GetProperty("bays").EnumerateArray().ToList();
        var first = bays.Single(b => b.GetProperty("bayId").GetGuid() == _bay1);
        Assert.That(first.GetProperty("bookings").GetArrayLength(), Is.EqualTo(2));
        Assert.That(first.GetProperty("utilisationPercent").GetDouble(), Is.EqualTo(50));
        Assert.That(first.GetProperty("freeGaps").EnumerateArray().Select(g => g.GetProperty("minutes").GetInt32()),
            Is.EqualTo(new[] { 120, 60, 60 }));
        Assert.That(board.GetProperty("utilisationPercent").GetDouble(), Is.EqualTo(25));
    }
}
