using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Modules.Marketplace;

/// <summary>
/// The public endpoints' output cache: a repeated URL is answered from the cache (it carries an Age header), other
/// URLs are not, errors are never cached, and an owner's edit is visible at once. Studio A is published.
/// </summary>
[TestFixture]
public class PublicCatalogCacheTests
{
    private ApiFactory _factory = null!;
    private TwoStudiosSeed _seed = null!;
    private HttpClient _owner = null!;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
        _factory = new ApiFactory();
        _owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        await SaveListingAsync("Cache Studio");
        (await _owner.PostAsync("/api/marketplace/listing/publish", null)).EnsureSuccessStatusCode();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        _owner.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task SaveListingAsync(string name) =>
        (await _owner.PutAsJsonAsync("/api/marketplace/listing", new
        {
            name, address = "ul. Krótka 2", city = "Gdańsk", latitude = 54.352, longitude = 18.646
        })).EnsureSuccessStatusCode();

    private static bool FromCache(HttpResponseMessage response) => response.Headers.Age.HasValue;

    [Test]
    public async Task ARepeatedSearch_IsServedFromTheCache_AndOtherAreasAreNot()
    {
        using var anonymous = _factory.CreateClient();
        var gdansk = $"/api/public/studios?minLat=54.3&maxLat=54.4&minLng=18.6&maxLng=18.7&pageSize={Random.Shared.Next(50, 100)}";
        var krakow = gdansk.Replace("minLat=54.3&maxLat=54.4", "minLat=50.0&maxLat=50.1");

        var first = await anonymous.GetAsync(gdansk);
        var second = await anonymous.GetAsync(gdansk);
        var elsewhere = await anonymous.GetAsync(krakow);

        Assert.That(FromCache(first), Is.False);
        Assert.That(FromCache(second), Is.True);
        Assert.That(FromCache(elsewhere), Is.False);
        Assert.That(await second.Content.ReadAsStringAsync(), Is.EqualTo(await first.Content.ReadAsStringAsync()));
        Assert.That(await IdsAsync(elsewhere), Does.Not.Contain(_seed.StudioA.StudioId));
    }

    [Test]
    public async Task SignedInCallers_ShareTheCachedAnswer()
    {
        using var anonymous = _factory.CreateClient();
        using var signedIn = _factory.CreateClientFor(_seed.StudioB.OwnerUserId);
        var url = $"/api/public/studios/{_seed.StudioA.StudioId}/reviews?limit={Random.Shared.Next(50, 100)}";

        await anonymous.GetAsync(url);
        var response = await signedIn.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(FromCache(response), Is.True);
    }

    [Test]
    public async Task NotFound_IsNeverCached()
    {
        using var anonymous = _factory.CreateClient();
        var url = $"/api/public/studios/{_seed.StudioB.StudioId}";

        await anonymous.GetAsync(url);
        var again = await anonymous.GetAsync(url);

        Assert.That(again.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(FromCache(again), Is.False);
    }

    [Test]
    public async Task AnOwnersEdit_ShowsAtOnce()
    {
        using var anonymous = _factory.CreateClient();
        var url = $"/api/public/studios/{_seed.StudioA.StudioId}";
        await anonymous.GetAsync(url);
        Assert.That(FromCache(await anonymous.GetAsync(url)), Is.True);

        await SaveListingAsync("Cache Studio Renamed");
        var after = await anonymous.GetFromJsonAsync<JsonElement>(url);

        Assert.That(after.GetProperty("name").GetString(), Is.EqualTo("Cache Studio Renamed"));
        await SaveListingAsync("Cache Studio");
    }

    private static async Task<List<Guid>> IdsAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray()
        .Select(p => p.GetProperty("studioId").GetGuid()).ToList();
}
