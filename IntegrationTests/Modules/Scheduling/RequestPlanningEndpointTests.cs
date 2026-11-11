using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Domain.Users;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Time;

namespace IntegrationTests.Modules.Scheduling;

/// <summary>
/// Customer requests at a studio that plans: one bay, open 08:00-16:00 every day, a two-hour wash (Medium) and a
/// 12-hour minimum lead time. The customers' cars are Large, so their visits take two and a half hours.
/// </summary>
[TestFixture]
public class RequestPlanningEndpointTests
{
    private static readonly TimeZoneInfo Warsaw = TimeZones.Find("Europe/Warsaw");

    private ApiFactory _factory = null!;
    private TwoStudiosSeed _seed = null!;
    private Guid _bay;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
        _factory = new ApiFactory();

        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        (await owner.PutAsJsonAsync($"/api/service/{_seed.StudioA.ServiceId}", new
        {
            name = "Mycie", category = "ExteriorWash", durationMinutes = 120,
            prices = new[] { new { vehicleSize = "Large", price = 100m } }
        })).EnsureSuccessStatusCode();
        var bay = await owner.PostAsJsonAsync("/api/bays", new { name = "Stanowisko" });
        _bay = (await bay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
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

    private static DateTime LocalAt(int daysAhead, int hour, int minute = 0) =>
        TimeZones.ToUtc(DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(hour).AddMinutes(minute), Warsaw);

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var code) ? code.GetString() : null;

    private async Task<HttpClient> NewCustomerAsync()
    {
        var userId = Guid.NewGuid();
        var email = $"{userId:N}@test.pl";
        await using (var db = PostgresFixture.CreateIdentityDbContext(new TenantContext()))
        {
            db.Users.Add(new User(userId, email, null, null));
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientFor(userId, email);
        (await client.PutAsJsonAsync("/api/me/profile", new { name = $"Klient {userId:N}"[..14], phone = "500600700" })).EnsureSuccessStatusCode();
        return client;
    }

    private Task<HttpResponseMessage> SubmitAsync(HttpClient customer, DateTime preferredAt) =>
        customer.PostAsJsonAsync("/api/me/booking-requests", new
        {
            studioId = _seed.StudioA.StudioId,
            serviceIds = new[] { _seed.StudioA.ServiceId },
            vehicle = new { licensePlate = $"KR {Random.Shared.Next(10000, 99999)}", size = "Large" },
            preferredAt
        });

    private async Task<Guid> RequestAsync(DateTime preferredAt)
    {
        using var customer = await NewCustomerAsync();
        var response = await SubmitAsync(customer, preferredAt);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Test]
    public async Task Request_OutsideOpeningHours_OrTooSoon_IsRefused()
    {
        using var customer = await NewCustomerAsync();

        // 14:00 + 2.5 h runs past the 16:00 closing; 07:00 is before opening; an hour from now is inside the lead time
        var pastClosing = await SubmitAsync(customer, LocalAt(3, 14));
        var beforeOpening = await SubmitAsync(customer, LocalAt(3, 7));
        var tooSoon = await SubmitAsync(customer, DateTime.UtcNow.AddHours(1));

        Assert.That(await ProblemCodeAsync(pastClosing), Is.EqualTo("OUTSIDE_OPENING_HOURS"));
        Assert.That(await ProblemCodeAsync(beforeOpening), Is.EqualTo("OUTSIDE_OPENING_HOURS"));
        Assert.That(await ProblemCodeAsync(tooSoon), Is.EqualTo("BOOKING_REQUEST_TOO_SOON"));
    }

    [Test]
    public async Task Plan_SaysThePreferredTimeFits_OnWhichBay()
    {
        var id = await RequestAsync(LocalAt(4, 9));
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var plan = await owner.GetFromJsonAsync<JsonElement>($"/api/booking-requests/{id}/plan");

        Assert.That(plan.GetProperty("planningEnabled").GetBoolean(), Is.True);
        Assert.That(plan.GetProperty("durationMinutes").GetInt32(), Is.EqualTo(150));
        var preferred = plan.GetProperty("preferred");
        Assert.That(preferred.GetProperty("fits").GetBoolean(), Is.True);
        Assert.That(preferred.GetProperty("bayId").GetGuid(), Is.EqualTo(_bay));
        Assert.That(preferred.GetProperty("end").GetDateTime(), Is.EqualTo(LocalAt(4, 11, 30)));
    }

    [Test]
    public async Task Plan_OfferTheClosestTimes_WhenThePreferredOneIsTaken_AndFlagsCompetingRequests()
    {
        var first = await RequestAsync(LocalAt(5, 10));
        var second = await RequestAsync(LocalAt(5, 10));
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        // Both want 10:00; until one is accepted neither holds the bay, so each sees the other as competing
        var before = await owner.GetFromJsonAsync<JsonElement>($"/api/booking-requests/{second}/plan");
        Assert.That(before.GetProperty("competingRequests").EnumerateArray().Select(r => r.GetProperty("id").GetGuid()),
            Does.Contain(first));
        Assert.That(before.GetProperty("preferred").GetProperty("fits").GetBoolean(), Is.True);

        (await owner.PostAsJsonAsync($"/api/booking-requests/{first}/accept", new { })).EnsureSuccessStatusCode();

        var after = await owner.GetFromJsonAsync<JsonElement>($"/api/booking-requests/{second}/plan");
        Assert.That(after.GetProperty("preferred").GetProperty("fits").GetBoolean(), Is.False);
        var alternatives = after.GetProperty("alternatives").EnumerateArray().ToList();
        Assert.That(alternatives, Has.Count.EqualTo(5));
        // 10:00-12:30 plus cleanup frees the bay at 12:45; 13:30 is the last start that ends by closing
        Assert.That(alternatives[0].GetProperty("start").GetDateTime(), Is.EqualTo(LocalAt(5, 12, 45)));
        Assert.That(alternatives.Select(a => a.GetProperty("minutesFromPreferred").GetInt32()), Is.Ordered);
    }

    [Test]
    public async Task Accepting_WhenTheOnlyBayIsTaken_LeavesTheRequestPending()
    {
        var first = await RequestAsync(LocalAt(6, 9));
        var second = await RequestAsync(LocalAt(6, 9));
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        (await owner.PostAsJsonAsync($"/api/booking-requests/{first}/accept", new { })).EnsureSuccessStatusCode();
        var refused = await owner.PostAsJsonAsync($"/api/booking-requests/{second}/accept", new { });
        var later = await owner.PostAsJsonAsync($"/api/booking-requests/{second}/accept", new { scheduledAt = LocalAt(6, 12) });

        Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ProblemCodeAsync(refused), Is.EqualTo("NO_BAY_AVAILABLE"));
        Assert.That(later.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task AnotherStudio_CannotPlanTheRequest()
    {
        var id = await RequestAsync(LocalAt(7, 9));
        using var other = _factory.CreateClientFor(_seed.StudioB.OwnerUserId);

        var response = await other.GetAsync($"/api/booking-requests/{id}/plan");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
