using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Modules.Scheduling;

/// <summary>
/// Setting a studio up for planning: its bays, weekly hours, rules and closed days.
/// </summary>
[TestFixture]
public class PlanningSetupEndpointTests
{
    private ApiFactory _factory = null!;
    private TwoStudiosSeed _seed = null!;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
        _factory = new ApiFactory();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync() => await _factory.DisposeAsync();

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var code) ? code.GetString() : null;

    [Test]
    public async Task Owner_ManagesBays_AndAnotherStudioNeverSeesThem()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        using var other = _factory.CreateClientFor(_seed.StudioB.OwnerUserId);

        var created = await owner.PostAsJsonAsync("/api/bays", new { name = " Stanowisko 1 " });
        Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var renamed = await owner.PutAsJsonAsync($"/api/bays/{id}", new { name = "Myjnia" });
        var othersRename = await other.PutAsJsonAsync($"/api/bays/{id}", new { name = "Moje" });
        var othersList = await other.GetFromJsonAsync<JsonElement>("/api/bays");

        Assert.That((await renamed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("name").GetString(), Is.EqualTo("Myjnia"));
        Assert.That(othersRename.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(othersList.EnumerateArray().Select(b => b.GetProperty("id").GetGuid()), Does.Not.Contain(id));

        Assert.That((await owner.DeleteAsync($"/api/bays/{id}")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var bay = (await owner.GetFromJsonAsync<JsonElement>("/api/bays")).EnumerateArray().Single(b => b.GetProperty("id").GetGuid() == id);
        Assert.That(bay.GetProperty("isActive").GetBoolean(), Is.False);
    }

    [Test]
    public async Task Bay_WithoutAName_IsRejected()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await owner.PostAsJsonAsync("/api/bays", new { name = "" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Schedule_TurnsPlanningOn_OnceThereAreHoursAndABay()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioB.OwnerUserId);

        var before = await owner.GetFromJsonAsync<JsonElement>("/api/schedule");
        Assert.That(before.GetProperty("planningEnabled").GetBoolean(), Is.False);
        Assert.That(before.GetProperty("openingHours").GetArrayLength(), Is.Zero);

        var saved = await owner.PutAsJsonAsync("/api/schedule", new
        {
            openingHours = new[]
            {
                new { day = "Monday", opens = "08:00:00", closes = "12:00:00" },
                new { day = "Monday", opens = "13:00:00", closes = "17:00:00" }
            },
            bufferMinutes = 20, slotStepMinutes = 30, minLeadHours = 24,
            durationMultipliers = new { small = 0.8m, medium = 1m, large = 1.3m, xl = 1.6m }
        });
        Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await saved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("planningEnabled").GetBoolean(), Is.False);

        (await owner.PostAsJsonAsync("/api/bays", new { name = "Stanowisko" })).EnsureSuccessStatusCode();

        var after = await owner.GetFromJsonAsync<JsonElement>("/api/schedule");
        Assert.Multiple(() =>
        {
            Assert.That(after.GetProperty("planningEnabled").GetBoolean(), Is.True);
            Assert.That(after.GetProperty("openingHours").GetArrayLength(), Is.EqualTo(2));
            Assert.That(after.GetProperty("bufferMinutes").GetInt32(), Is.EqualTo(20));
            Assert.That(after.GetProperty("durationMultipliers").GetProperty("xl").GetDecimal(), Is.EqualTo(1.6m));
        });
    }

    [Test]
    public async Task Schedule_WithOverlappingHours_IsRejectedWithACode()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await owner.PutAsJsonAsync("/api/schedule", new
        {
            openingHours = new[]
            {
                new { day = "Tuesday", opens = "08:00:00", closes = "12:00:00" },
                new { day = "Tuesday", opens = "11:00:00", closes = "15:00:00" }
            }
        });

        Assert.That(await ProblemCodeAsync(response), Is.EqualTo("OVERLAPPING_OPENING_PERIODS"));
    }

    [Test]
    public async Task ClosedDays_CanBeAddedAndRemoved()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var closed = await owner.PostAsJsonAsync("/api/schedule/closures", new { date = "2026-12-24", reason = "Wigilia" });
        var schedule = await closed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(schedule.GetProperty("closures").EnumerateArray().Select(c => c.GetProperty("date").GetString()),
            Does.Contain("2026-12-24"));

        Assert.That((await owner.DeleteAsync("/api/schedule/closures/2026-12-24")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var again = await owner.DeleteAsync("/api/schedule/closures/2026-12-24");
        Assert.That(await ProblemCodeAsync(again), Is.EqualTo("CLOSURE_NOT_FOUND"));
    }
}
