using Inventory.Application.Categories;
using Inventory.Application.Categories.CreateCategory;
using Inventory.Application.Categories.DeleteCategory;
using Inventory.Application.Categories.GetCategory;
using Inventory.Application.Categories.UpdateCategory;
using Inventory.Application.Items;
using Inventory.Application.Items.CreateItem;
using Inventory.Application.Items.GetItem;
using Inventory.Application.Items.GetItems;
using Inventory.Application.Items.UpdateItem;
using IntegrationTests.Infrastructure;
using static IntegrationTests.Modules.Inventory.InventoryTenantIsolationTests;

namespace IntegrationTests.Modules.Inventory;

[TestFixture]
public class InventoryHandlerTests
{
    private TwoStudiosSeed _seed = null!;
    private Guid StudioId => _seed.StudioA.StudioId;
    private static CancellationToken Ct => CancellationToken.None;

    [OneTimeSetUp]
    public async Task SeedAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
    }

    [Test]
    public async Task CreateItem_StampsStudioAndReturnsCategoryName()
    {
        await using var db = DbForStudio(StudioId);

        var created = await new CreateItemHandler(db).HandleAsync(new InventoryCreateDto
        {
            Name = "Szampon", Unit = "l", Stock = 4, MinStock = 2, CategoryId = _seed.StudioA.InventoryCategoryId
        }, Ct);

        Assert.That(created.StudioId, Is.EqualTo(StudioId));
        Assert.That(created.CategoryName, Is.EqualTo("Chemicals"));

        await using var readDb = DbForStudio(StudioId);
        var reloaded = await new GetItemHandler(readDb).HandleAsync(created.Id, Ct);
        Assert.That(reloaded.Value!.Name, Is.EqualTo("Szampon"));
    }

    [Test]
    public async Task UpdateItem_ChangesFields()
    {
        await using var db = DbForStudio(StudioId);
        var created = await new CreateItemHandler(db).HandleAsync(
            new InventoryCreateDto { Name = "Pasta", Unit = "szt", Stock = 1 }, Ct);

        var result = await new UpdateItemHandler(db).HandleAsync(created.Id,
            new InventoryCreateDto { Name = "Pasta polerska", Unit = "szt", Stock = 7, MinStock = 3 }, Ct);

        Assert.That(result.Value!.Name, Is.EqualTo("Pasta polerska"));
        Assert.That(result.Value.Stock, Is.EqualTo(7));
        Assert.That(result.Value.MinStock, Is.EqualTo(3));
    }

    [Test]
    public async Task Item_KeepsFractionalQuantities()
    {
        await using var db = DbForStudio(StudioId);

        var created = await new CreateItemHandler(db).HandleAsync(
            new InventoryCreateDto { Name = "Wosk", Unit = "l", Stock = 0.75m, MinStock = 0.5m }, Ct);

        await using var readDb = DbForStudio(StudioId);
        var reloaded = await new GetItemHandler(readDb).HandleAsync(created.Id, Ct);
        Assert.That(reloaded.Value!.Stock, Is.EqualTo(0.75m));
        Assert.That(reloaded.Value.MinStock, Is.EqualTo(0.5m));
    }

    [Test]
    public async Task GetItems_LowStockOnly_ReturnsItemsBelowMinimum()
    {
        await using var db = DbForStudio(StudioId);
        var create = new CreateItemHandler(db);
        var low = await create.HandleAsync(new InventoryCreateDto { Name = "Glinka", Unit = "szt", Stock = 1, MinStock = 4 }, Ct);
        var enough = await create.HandleAsync(new InventoryCreateDto { Name = "Recznik", Unit = "szt", Stock = 9, MinStock = 4 }, Ct);

        var lowStock = await new GetItemsHandler(db).HandleAsync(lowStockOnly: true, Ct);

        Assert.That(lowStock.Select(i => i.Id), Does.Contain(low.Id));
        Assert.That(lowStock.Select(i => i.Id), Does.Not.Contain(enough.Id));
    }

    [Test]
    public async Task Category_CanBeCreatedRenamedAndDeleted()
    {
        await using var db = DbForStudio(StudioId);

        var category = await new CreateCategoryHandler(db).HandleAsync(new CategoryCreateDto { Name = "Akcesoria" }, Ct);
        var renamed = await new UpdateCategoryHandler(db).HandleAsync(category.Id, new CategoryCreateDto { Name = "Mikrofibry" }, Ct);
        var deleted = await new DeleteCategoryHandler(db).HandleAsync(category.Id, Ct);
        var afterDelete = await new GetCategoryHandler(db).HandleAsync(category.Id, Ct);

        Assert.That(renamed.Value!.Name, Is.EqualTo("Mikrofibry"));
        Assert.That(deleted.Success, Is.True);
        Assert.That(afterDelete.ErrorCode, Is.EqualTo("INVENTORY_CATEGORY_NOT_FOUND"));
    }

    [Test]
    public async Task DeleteCategory_KeepsItsItemsWithoutCategory()
    {
        await using var db = DbForStudio(StudioId);
        var category = await new CreateCategoryHandler(db).HandleAsync(new CategoryCreateDto { Name = "Do usuniecia" }, Ct);
        var item = await new CreateItemHandler(db).HandleAsync(
            new InventoryCreateDto { Name = "Pad", Unit = "szt", Stock = 2, CategoryId = category.Id }, Ct);

        await new DeleteCategoryHandler(db).HandleAsync(category.Id, Ct);

        await using var readDb = DbForStudio(StudioId);
        var reloaded = await new GetItemHandler(readDb).HandleAsync(item.Id, Ct);
        Assert.That(reloaded.Success, Is.True);
        Assert.That(reloaded.Value!.CategoryName, Is.Null);
    }
}
