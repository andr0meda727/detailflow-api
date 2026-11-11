using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Api;

/// <summary>
/// List endpoints share one envelope: { items, page, pageSize, totalCount, totalPages }.
/// </summary>
[TestFixture]
public class PagedListEndpointTests
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

    [TestCase("/api/clients")]
    [TestCase("/api/vehicle")]
    [TestCase("/api/bookings")]
    [TestCase("/api/inventory")]
    public async Task List_ReturnsPagedEnvelope(string path)
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var page = await client.GetFromJsonAsync<JsonElement>($"{path}?page=1&pageSize=1");

        Assert.That(page.GetProperty("items").GetArrayLength(), Is.EqualTo(1));
        Assert.That(page.GetProperty("page").GetInt32(), Is.EqualTo(1));
        Assert.That(page.GetProperty("pageSize").GetInt32(), Is.EqualTo(1));
        Assert.That(page.GetProperty("totalCount").GetInt32(), Is.EqualTo(1));
        Assert.That(page.GetProperty("totalPages").GetInt32(), Is.EqualTo(1));
    }

    [TestCase("/api/clients?pageSize=0")]
    [TestCase("/api/vehicle?page=-1")]
    public async Task List_WithInvalidPaging_ReturnsValidationProblem(string url)
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
    }
}
