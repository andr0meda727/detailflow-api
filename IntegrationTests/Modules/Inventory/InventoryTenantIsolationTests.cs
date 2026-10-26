using Inventory.Application.Categories.GetCategories;
using Inventory.Application.Items;
using Inventory.Application.Items.GetItem;
using Inventory.Application.Items.GetItems;
using Inventory.Application.Items.UpdateItem;
using Inventory.Infrastructure.Persistence;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.Modules.Inventory;

[TestFixture]
public class InventoryTenantIsolationTests
{
    private TwoStudiosSeed _seed = null!;

    [OneTimeSetUp]
    public async Task SeedAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
    }

    internal static InventoryDbContext DbForStudio(Guid studioId)
    {
        var tenant = new TenantContext();
        tenant.SetTenant(studioId, Guid.NewGuid());
        return PostgresFixture.CreateInventoryDbContext(tenant);
    }

    private static InventoryDbContext DbForUserWithoutStudio()
    {
        var tenant = new TenantContext();
        tenant.SetUserOnly(Guid.NewGuid());
        return PostgresFixture.CreateInventoryDbContext(tenant);
    }

    [Test]
    public async Task Categories_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var categories = await new GetCategoriesHandler(db).HandleAsync(CancellationToken.None);

        Assert.That(categories.Select(c => c.Id), Does.Contain(_seed.StudioA.InventoryCategoryId));
        Assert.That(categories.Select(c => c.Id), Does.Not.Contain(_seed.StudioB.InventoryCategoryId));
    }

    [Test]
    public async Task Items_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);
        var handler = new GetItemsHandler(db);

        var all = await handler.HandleAsync(lowStockOnly: false, CancellationToken.None);
        var lowStock = await handler.HandleAsync(lowStockOnly: true, CancellationToken.None);

        Assert.That(all.Select(i => i.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
        Assert.That(all.Select(i => i.Id), Does.Contain(_seed.StudioA.InventoryId));
        Assert.That(lowStock.Select(i => i.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
    }

    [Test]
    public async Task GetItem_DoesNotReturnOtherStudiosItem()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var result = await new GetItemHandler(db).HandleAsync(_seed.StudioB.InventoryId, CancellationToken.None);

        Assert.That(result.ErrorCode, Is.EqualTo("INVENTORY_ITEM_NOT_FOUND"));
    }

    [Test]
    public async Task UserWithoutStudio_SeesNoInventory()
    {
        await using var db = DbForUserWithoutStudio();

        Assert.Multiple(async () =>
        {
            Assert.That(await new GetCategoriesHandler(db).HandleAsync(CancellationToken.None), Is.Empty);
            Assert.That(await new GetItemsHandler(db).HandleAsync(false, CancellationToken.None), Is.Empty);
        });
    }

    [Test]
    public async Task UpdateItem_CannotOverwriteOtherStudiosItem()
    {
        await using (var db = DbForStudio(_seed.StudioA.StudioId))
        {
            var result = await new UpdateItemHandler(db).HandleAsync(_seed.StudioB.InventoryId,
                new InventoryCreateDto { Name = "Hijacked", Unit = "l", Stock = 0 }, CancellationToken.None);

            Assert.That(result.ErrorCode, Is.EqualTo("INVENTORY_ITEM_NOT_FOUND"));
        }

        await using (var db = DbForStudio(_seed.StudioB.StudioId))
        {
            var item = await new GetItemHandler(db).HandleAsync(_seed.StudioB.InventoryId, CancellationToken.None);
            Assert.That(item.Value!.Name, Is.EqualTo("Wax"));
        }
    }
}
