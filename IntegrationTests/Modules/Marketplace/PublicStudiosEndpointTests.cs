using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Domain.Studios;
using Identity.Domain.Users;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Marketplace.Application.Directory;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.Modules.Marketplace;

/// <summary>
/// The customer map: anonymous search by area, category and vehicle size, and the public studio page.
/// Studio A is published in Kraków with priced services; studio B has a listing in Kraków too but stays unpublished.
/// </summary>
[TestFixture]
public class PublicStudiosEndpointTests
{
    // A box around Kraków, and one around Warsaw where neither studio is
    private const string Krakow = "minLat=50.0&maxLat=50.1&minLng=19.8&maxLng=20.1";
    private const string Warsaw = "minLat=52.1&maxLat=52.3&minLng=20.9&maxLng=21.1";

    private ApiFactory _factory = null!;
    private TwoStudiosSeed _seed = null!;
    private Guid _ceramicId;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
        _factory = new ApiFactory();

        using var ownerA = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        (await ownerA.PutAsJsonAsync($"/api/service/{_seed.StudioA.ServiceId}", new
        {
            name = "Mycie", category = "ExteriorWash",
            prices = new[] { new { vehicleSize = "Medium", price = 80m }, new { vehicleSize = "Large", price = 100m } }
        })).EnsureSuccessStatusCode();
        var ceramic = await ownerA.PostAsJsonAsync("/api/service", new
        {
            name = "Ceramika", category = "Ceramic", prices = new[] { new { vehicleSize = "Large", price = 1500m } }
        });
        _ceramicId = (await ceramic.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Created after the services, so the listing copies them straight away
        (await ownerA.PutAsJsonAsync("/api/marketplace/listing", Listing("Studio A", 50.06, 19.94))).EnsureSuccessStatusCode();
        (await ownerA.PostAsync("/api/marketplace/listing/publish", null)).EnsureSuccessStatusCode();

        using var ownerB = _factory.CreateClientFor(_seed.StudioB.OwnerUserId);
        (await ownerB.PutAsJsonAsync("/api/marketplace/listing", Listing("Studio B", 50.07, 19.95))).EnsureSuccessStatusCode();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync() => await _factory.DisposeAsync();

    private static object Listing(string name, double latitude, double longitude) =>
        new { name, address = "ul. Długa 1", city = "Kraków", latitude, longitude };

    private async Task<List<JsonElement>> SearchAsync(string query, HttpClient? client = null)
    {
        var http = client ?? _factory.CreateClient();
        var page = await http.GetFromJsonAsync<JsonElement>($"/api/public/studios?{query}");
        return page.GetProperty("items").EnumerateArray()
            .Where(s => s.GetProperty("studioId").GetGuid() == _seed.StudioA.StudioId
                || s.GetProperty("studioId").GetGuid() == _seed.StudioB.StudioId)
            .ToList();
    }

    [Test]
    public async Task Search_FindsOnlyPublishedStudiosInTheArea_WithoutSigningIn()
    {
        var inKrakow = await SearchAsync(Krakow);
        var inWarsaw = await SearchAsync(Warsaw);

        Assert.That(inKrakow.Select(s => s.GetProperty("studioId").GetGuid()), Is.EqualTo(new[] { _seed.StudioA.StudioId }));
        Assert.That(inKrakow[0].GetProperty("latitude").GetDouble(), Is.EqualTo(50.06));
        Assert.That(inKrakow[0].GetProperty("categories").EnumerateArray().Select(c => c.GetString()),
            Is.EqualTo(new[] { "ExteriorWash", "Ceramic" }));
        Assert.That(inWarsaw, Is.Empty);
    }

    [Test]
    public async Task Search_ByCategoryAndSize_ShowsTheCheapestMatchingPrice()
    {
        var anyLarge = await SearchAsync($"{Krakow}&vehicleSize=Large");
        var ceramicLarge = await SearchAsync($"{Krakow}&category=Ceramic&vehicleSize=Large");
        var ceramicSmall = await SearchAsync($"{Krakow}&category=Ceramic&vehicleSize=Small");
        var interior = await SearchAsync($"{Krakow}&category=Interior");

        Assert.That(anyLarge.Single().GetProperty("fromPrice").GetDecimal(), Is.EqualTo(100m));
        Assert.That(ceramicLarge.Single().GetProperty("fromPrice").GetDecimal(), Is.EqualTo(1500m));
        Assert.That(ceramicSmall.Single().GetProperty("fromPrice").ValueKind, Is.EqualTo(JsonValueKind.Null), "price on request");
        Assert.That(interior, Is.Empty);
    }

    [Test]
    public async Task StudioPage_ShowsServicesWithPrices_ButNotUnpublishedStudios()
    {
        var anonymous = _factory.CreateClient();

        var page = await anonymous.GetFromJsonAsync<JsonElement>($"/api/public/studios/{_seed.StudioA.StudioId}");
        var hidden = await anonymous.GetAsync($"/api/public/studios/{_seed.StudioB.StudioId}");

        var ceramic = page.GetProperty("services").EnumerateArray().Single(s => s.GetProperty("id").GetGuid() == _ceramicId);
        Assert.That(ceramic.GetProperty("prices")[0].GetProperty("vehicleSize").GetString(), Is.EqualTo("Large"));
        Assert.That(hidden.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PublicStudios_KnowsOnlyPublishedStudios()
    {
        await using var db = PostgresFixture.CreateMarketplaceDbContext(new TenantContext());
        var studios = new PublicStudios(db);

        Assert.That((await studios.FindPublishedAsync(_seed.StudioA.StudioId))?.Name, Is.EqualTo("Studio A"));
        Assert.That(await studios.FindPublishedAsync(_seed.StudioB.StudioId), Is.Null);
    }

    [Test]
    public async Task StaffOfTwoStudios_CanSearchWithoutChoosingOne()
    {
        // Elsewhere such a user must send X-Studio-Id; the public map doesn't care about studios
        var userId = Guid.NewGuid();
        await using (var db = PostgresFixture.CreateIdentityDbContext(new TenantContext()))
        {
            db.Users.Add(new User(userId, $"{userId:N}@test.pl", null, null));
            foreach (var studioId in new[] { _seed.StudioA.StudioId, _seed.StudioB.StudioId })
            {
                var studio = await db.Studios.FindAsync(studioId);
                await db.Entry(studio!).Collection(s => s.Members).LoadAsync();
                studio!.Admit(userId, Roles.Employee);
            }
            await db.SaveChangesAsync();
        }
        using var client = _factory.CreateClientFor(userId);

        var response = await client.GetAsync($"/api/public/studios?{Krakow}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [TestCase("minLat=50.1&maxLat=50.0&minLng=19.8&maxLng=20.1")]
    [TestCase("minLat=50.0&maxLat=95&minLng=19.8&maxLng=20.1")]
    [TestCase("minLat=50.0&maxLat=50.1&minLng=19.8&maxLng=20.1&category=Painting")]
    public async Task Search_WithAnInvalidArea_IsAValidationProblem(string query)
    {
        var response = await _factory.CreateClient().GetAsync($"/api/public/studios?{query}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task TooManyRequestsFromOneClient_AreRejected()
    {
        await using var limited = new ApiFactory(new Dictionary<string, string?> { ["RateLimiting:PublicPermitLimit"] = "3" });
        var client = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++) statuses.Add((await client.GetAsync($"/api/public/studios?{Warsaw}")).StatusCode);

        Assert.That(statuses, Is.EqualTo(new[] { HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests }));
    }
}
