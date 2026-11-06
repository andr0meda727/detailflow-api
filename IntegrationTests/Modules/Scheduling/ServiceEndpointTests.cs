using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Modules.Scheduling;

[TestFixture]
public class ServiceEndpointTests
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

    [Test]
    public async Task CategoryAndSizes_TravelAsNames()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PostAsJsonAsync("/api/service", new
        {
            name = "Pranie tapicerki",
            category = "Interior",
            prices = new[] { new { vehicleSize = "Large", price = 349.99m } }
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("category").GetString(), Is.EqualTo("Interior"));
        Assert.That(body.GetProperty("prices")[0].GetProperty("vehicleSize").GetString(), Is.EqualTo("Large"));
        Assert.That(body.GetProperty("prices")[0].GetProperty("price").GetDecimal(), Is.EqualTo(349.99m));
    }

    [Test]
    public async Task NameOnly_StillWorks_AsOtherWithoutPrices()
    {
        // What the current frontend sends
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PostAsJsonAsync("/api/service", new { name = "Woskowanie" });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("category").GetString(), Is.EqualTo("Other"));
        Assert.That(body.GetProperty("prices").GetArrayLength(), Is.Zero);
    }

    [Test]
    public async Task UnknownCategory_IsAValidationProblem()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PostAsJsonAsync("/api/service", new { name = "X", category = "Painting" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Update_ChangesTheService()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PutAsJsonAsync($"/api/service/{_seed.StudioA.ServiceId}", new
        {
            name = "Polerowanie jednoetapowe",
            category = "Polishing",
            prices = new[] { new { vehicleSize = "Small", price = 600m }, new { vehicleSize = "XL", price = 1100m } }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var services = await client.GetFromJsonAsync<JsonElement>("/api/service");
        var service = services.EnumerateArray().Single(s => s.GetProperty("id").GetGuid() == _seed.StudioA.ServiceId);
        Assert.That(service.GetProperty("category").GetString(), Is.EqualTo("Polishing"));
        Assert.That(service.GetProperty("prices").GetArrayLength(), Is.EqualTo(2));
    }
}
