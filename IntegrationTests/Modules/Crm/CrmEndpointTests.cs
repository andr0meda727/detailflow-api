using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Modules.Crm;

[TestFixture]
public class CrmEndpointTests
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
    public async Task CreateClient_ReturnsCreatedWithTheExistingResponseShape()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        var phone = Random.Shared.Next(100_000_000, 999_999_999).ToString();

        var response = await client.PostAsJsonAsync("/api/clients", new
        {
            name = "Anna", phone, vehicle = new { licensePlate = "KR 5", brand = "Fiat", model = "500" }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("phone").GetString(), Is.EqualTo(phone));
        Assert.That(body.GetProperty("vehicles")[0].GetProperty("licensePlate").GetString(), Is.EqualTo("KR 5"));
        Assert.That(response.Headers.Location?.ToString(), Does.EndWith(body.GetProperty("id").GetString()));
    }

    [Test]
    public async Task CreateClient_WithoutPhone_IsAValidationProblem()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PostAsJsonAsync("/api/clients", new { name = "Bez telefonu" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreateClient_WithTakenPhone_IsAConflictWithCode()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PostAsJsonAsync("/api/clients", new { name = "Jan", phone = _seed.SharedClientPhone });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(problem.GetProperty("code").GetString(), Is.EqualTo("CLIENT_PHONE_TAKEN"));
    }

    [Test]
    public async Task CreateVehicle_ForOtherStudiosClient_IsRejected()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PostAsJsonAsync("/api/vehicle",
            new { clientId = _seed.StudioB.ClientId, licensePlate = "KR 9" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetOtherStudiosClient_ReturnsNotFound()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.GetAsync($"/api/clients/{_seed.StudioB.ClientId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
