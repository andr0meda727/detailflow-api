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
/// Bookings in a studio that plans: they get an end from the services and a bay, and the database never lets two of
/// them share a bay at once. Studio A has two bays, is open 08:00-18:00 every day and its service takes two hours.
/// Every test uses its own day, so they don't take each other's bays.
/// </summary>
[TestFixture]
public class BookingPlacementEndpointTests
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
        _bay1 = await CreateBayAsync(owner, "Stanowisko 1");
        _bay2 = await CreateBayAsync(owner, "Stanowisko 2");
        (await owner.PutAsJsonAsync("/api/schedule", new
        {
            openingHours = Enum.GetValues<DayOfWeek>().Select(day => new { day = day.ToString(), opens = "08:00:00", closes = "18:00:00" }),
            bufferMinutes = 15
        })).EnsureSuccessStatusCode();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync() => await _factory.DisposeAsync();

    private static async Task<Guid> CreateBayAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/bays", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    // A local time in the studio's zone, some days ahead
    private static DateTime LocalAt(int daysAhead, int hour) =>
        TimeZones.ToUtc(DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(hour), Warsaw);

    private Task<HttpResponseMessage> BookAsync(HttpClient client, DateTime at, Guid? bayId = null) =>
        client.PostAsJsonAsync("/api/bookings", new BookingRequest
        {
            ClientId = _seed.StudioA.ClientId,
            VehicleId = _seed.StudioA.VehicleId,
            StatusId = BookingStatus.Created,
            ScheduledAt = at,
            ServiceIds = [_seed.StudioA.ServiceId],
            Price = 300m,
            BayId = bayId
        });

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var code) ? code.GetString() : null;

    [Test]
    public async Task Booking_GetsItsLength_AndABay_UntilTheBaysRunOut()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        var at = LocalAt(3, 9);

        var first = await (await BookAsync(owner, at)).Content.ReadFromJsonAsync<JsonElement>();
        var second = await (await BookAsync(owner, at)).Content.ReadFromJsonAsync<JsonElement>();
        var third = await BookAsync(owner, at);

        Assert.That(first.GetProperty("endsAt").GetDateTime(), Is.EqualTo(at.AddHours(2)));
        var bays = new[] { first, second }.Select(b => b.GetProperty("bay").GetProperty("id").GetGuid()).ToList();
        Assert.That(bays, Is.EquivalentTo(new[] { _bay1, _bay2 }));
        Assert.That(third.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ProblemCodeAsync(third), Is.EqualTo("NO_BAY_AVAILABLE"));
    }

    [Test]
    public async Task ChosenBay_ThatIsTaken_IsRefused_IncludingItsCleanupTime()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        (await BookAsync(owner, LocalAt(4, 9), _bay1)).EnsureSuccessStatusCode();

        // 09:00-11:00 plus 15 minutes of cleanup: 11:00 is too soon on the same bay, 11:15 is fine
        var tooSoon = await BookAsync(owner, LocalAt(4, 11), _bay1);
        var afterCleanup = await BookAsync(owner, LocalAt(4, 11).AddMinutes(15), _bay1);

        Assert.That(await ProblemCodeAsync(tooSoon), Is.EqualTo("BAY_TAKEN"));
        Assert.That(afterCleanup.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task CancellingABooking_FreesItsBay()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        var at = LocalAt(5, 12);
        var id = (await (await BookAsync(owner, at, _bay2)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.That(await ProblemCodeAsync(await BookAsync(owner, at, _bay2)), Is.EqualTo("BAY_TAKEN"));
        (await owner.PostAsync($"/api/bookings/{id}/cancel", null)).EnsureSuccessStatusCode();

        Assert.That((await BookAsync(owner, at, _bay2)).StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task ParallelRequestsForOneBay_OnlyOneWins()
    {
        // The planner checks first, but requests that check at the same moment all see the bay free; the exclusion
        // constraint lets exactly one of them in
        var at = LocalAt(6, 10);
        var clients = Enumerable.Range(0, 10).Select(_ => _factory.CreateClientFor(_seed.StudioA.OwnerUserId)).ToList();

        var responses = await Task.WhenAll(clients.Select(c => BookAsync(c, at, _bay1)));

        Assert.That(responses.Count(r => r.StatusCode == HttpStatusCode.Created), Is.EqualTo(1));
        var refused = responses.Where(r => r.StatusCode != HttpStatusCode.Created).ToList();
        Assert.That(refused.Select(r => r.StatusCode), Is.All.EqualTo(HttpStatusCode.Conflict));
        foreach (var response in refused) Assert.That(await ProblemCodeAsync(response), Is.EqualTo("BAY_TAKEN"));
        clients.ForEach(c => c.Dispose());
    }

    [Test]
    public async Task Staff_CanBookOutsideOpeningHours_OnAFreeBay()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var late = await BookAsync(owner, LocalAt(7, 19));

        Assert.That(late.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That((await late.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("bay").ValueKind, Is.EqualTo(JsonValueKind.Object));
    }

    [Test]
    public async Task MovingABooking_KeepsItsBay_WhenItIsStillFree()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        var created = await (await BookAsync(owner, LocalAt(8, 9), _bay2)).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var moved = await owner.PutAsJsonAsync($"/api/bookings/{id}", new BookingRequest
        {
            ClientId = _seed.StudioA.ClientId, VehicleId = _seed.StudioA.VehicleId, StatusId = BookingStatus.Created,
            ScheduledAt = LocalAt(8, 13), ServiceIds = [_seed.StudioA.ServiceId], Price = 300m
        });

        var body = await moved.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("bay").GetProperty("id").GetGuid(), Is.EqualTo(_bay2));
        Assert.That(body.GetProperty("endsAt").GetDateTime(), Is.EqualTo(LocalAt(8, 15)));
    }

    [Test]
    public async Task AnotherStudiosBay_IsRejected()
    {
        using var other = _factory.CreateClientFor(_seed.StudioB.OwnerUserId);

        var response = await other.PostAsJsonAsync("/api/bookings", new BookingRequest
        {
            ClientId = _seed.StudioB.ClientId, VehicleId = _seed.StudioB.VehicleId, StatusId = BookingStatus.Created,
            ScheduledAt = LocalAt(9, 9), ServiceIds = [_seed.StudioB.ServiceId], Price = 100m, BayId = _bay1
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
