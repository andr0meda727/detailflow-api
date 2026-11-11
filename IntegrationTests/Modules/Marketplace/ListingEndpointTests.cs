using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Domain.Studios;
using Identity.Domain.Users;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.Modules.Marketplace;

/// <summary>
/// The owner's side of the studio listing: create, edit, publish.
/// </summary>
[TestFixture]
public class ListingEndpointTests
{
    private ApiFactory _factory = null!;
    private TwoStudiosSeed _seed = null!;

    private static readonly object Krakow = new
    {
        name = "Detail Studio Kraków", description = "Mycie i ceramika", address = "ul. Długa 1", city = "Kraków",
        phone = "500600700", latitude = 50.0614, longitude = 19.9366
    };

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

    private async Task<Guid> AddEmployeeAsync(Guid studioId)
    {
        var userId = Guid.NewGuid();
        await using var db = PostgresFixture.CreateIdentityDbContext(new TenantContext());
        db.Users.Add(new User(userId, $"{userId:N}@test.pl", null, null));
        var studio = await db.Studios.FindAsync(studioId);
        await db.Entry(studio!).Collection(s => s.Members).LoadAsync();
        studio!.Admit(userId, Roles.Employee);
        await db.SaveChangesAsync();
        return userId;
    }

    [Test]
    public async Task Owner_CreatesTheListing_WithTheStudiosServices_Unpublished()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var before = await owner.GetAsync("/api/marketplace/listing");
        var saved = await owner.PutAsJsonAsync("/api/marketplace/listing", Krakow);

        Assert.That(before.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await ProblemCodeAsync(before), Is.EqualTo("LISTING_NOT_FOUND"));
        Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var listing = await owner.GetFromJsonAsync<JsonElement>("/api/marketplace/listing");
        Assert.That(listing.GetProperty("name").GetString(), Is.EqualTo("Detail Studio Kraków"));
        Assert.That(listing.GetProperty("isPublished").GetBoolean(), Is.False);
        Assert.That(listing.GetProperty("services").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()),
            Is.EqualTo(new[] { _seed.StudioA.ServiceId }));

        // Publishing shows it; it has a service, so that's allowed
        var publish = await owner.PostAsync("/api/marketplace/listing/publish", null);
        Assert.That(publish.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var published = await owner.GetFromJsonAsync<JsonElement>("/api/marketplace/listing");
        Assert.That(published.GetProperty("isPublished").GetBoolean(), Is.True);
    }

    [Test]
    public async Task StudioWithoutServices_CannotBePublished()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioB.OwnerUserId);
        await owner.DeleteAsync($"/api/service/{_seed.StudioB.ServiceId}");
        await owner.PutAsJsonAsync("/api/marketplace/listing", Krakow);

        var publish = await owner.PostAsync("/api/marketplace/listing/publish", null);

        Assert.That(publish.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await ProblemCodeAsync(publish), Is.EqualTo("LISTING_HAS_NO_SERVICES"));
    }

    [Test]
    public async Task Employee_CanSeeButNotChangeTheListing()
    {
        using var employee = _factory.CreateClientFor(await AddEmployeeAsync(_seed.StudioA.StudioId));

        var save = await employee.PutAsJsonAsync("/api/marketplace/listing", Krakow);
        var publish = await employee.PostAsync("/api/marketplace/listing/publish", null);

        Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(publish.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task APinOffTheMap_IsAValidationProblem()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await owner.PutAsJsonAsync("/api/marketplace/listing", new
        {
            name = "X", address = "Y", city = "Z", latitude = 95.0, longitude = 19.9
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
