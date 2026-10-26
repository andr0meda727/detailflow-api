using System.Net;
using System.Net.Http.Json;
using Inventory.Application.Items;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Modules.Inventory;

[TestFixture]
public class InventoryEndpointTests
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
    public async Task GetInventory_ReturnsOnlyOwnStudiosItems()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var items = await client.GetFromJsonAsync<List<InventoryResponseDto>>("/api/inventory");

        Assert.That(items!.Select(i => i.Id), Does.Contain(_seed.StudioA.InventoryId));
        Assert.That(items!.Select(i => i.Id), Does.Not.Contain(_seed.StudioB.InventoryId));
    }

    [Test]
    public async Task GetOtherStudiosItem_ReturnsNotFound()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.GetAsync($"/api/inventory/{_seed.StudioB.InventoryId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CreateItem_InOtherStudiosCategory_IsRejected()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PostAsJsonAsync("/api/inventory",
            new InventoryCreateDto { Name = "Foam", Unit = "l", Stock = 3, CategoryId = _seed.StudioB.InventoryCategoryId });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
