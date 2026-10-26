using System.Net;
using System.Net.Http.Json;
using Inventory.Application.Items;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Shared.Kernel.Pagination;

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

        var items = await client.GetFromJsonAsync<PagedResponse<InventoryResponseDto>>("/api/inventory");

        Assert.That(items!.Items.Select(i => i.Id), Does.Contain(_seed.StudioA.InventoryId));
        Assert.That(items!.Items.Select(i => i.Id), Does.Not.Contain(_seed.StudioB.InventoryId));
    }

    [Test]
    public async Task GetInventory_ReturnsRequestedPage()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var page = await client.GetFromJsonAsync<PagedResponse<InventoryResponseDto>>("/api/inventory?page=1&pageSize=1");

        Assert.That(page!.Items, Has.Count.EqualTo(1));
        Assert.That(page, Has.Property(nameof(page.Page)).EqualTo(1).And.Property(nameof(page.PageSize)).EqualTo(1));
        Assert.That(page.TotalCount, Is.GreaterThanOrEqualTo(1));
    }

    [TestCase("pageSize=101")]
    [TestCase("page=0")]
    public async Task GetInventory_WithInvalidPaging_ReturnsValidationProblem(string query)
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.GetAsync($"/api/inventory?{query}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
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
