using Inventory.Application.Items;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories;
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

    private static InventoryDbContext DbForStudio(Guid studioId)
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

        var categories = await new InventoryCategoryRepository(db).GetAllAsync();

        Assert.That(categories.Select(c => c.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
        Assert.That(categories.Select(c => c.Id), Does.Contain(_seed.StudioA.InventoryCategoryId));
    }

    [Test]
    public async Task Items_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);
        var repository = new InventoryRepository(db);

        var all = await repository.GetAllAsync();
        var lowStock = await repository.GetLowStockAsync();

        Assert.That(all.Select(i => i.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
        Assert.That(all.Select(i => i.Id), Does.Contain(_seed.StudioA.InventoryId));
        Assert.That(lowStock.Select(i => i.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
    }

    [Test]
    public async Task GetById_DoesNotReturnOtherStudiosItem()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        Assert.That(await new InventoryRepository(db).GetByIdAsync(_seed.StudioB.InventoryId), Is.Null);
    }

    [Test]
    public async Task UserWithoutStudio_SeesNoInventory()
    {
        await using var db = DbForUserWithoutStudio();

        Assert.Multiple(async () =>
        {
            Assert.That(await new InventoryCategoryRepository(db).GetAllAsync(), Is.Empty);
            Assert.That(await new InventoryRepository(db).GetAllAsync(), Is.Empty);
        });
    }

    [Test]
    public async Task EditInventory_CannotOverwriteOtherStudiosItem()
    {
        await using (var db = DbForStudio(_seed.StudioA.StudioId))
        {
            var service = new InventoryService(new InventoryRepository(db), new InventoryMapper());

            var result = await service.EditInventory(_seed.StudioB.InventoryId,
                new InventoryCreateDto { Name = "Hijacked", Unit = "l", Stock = 0 });

            Assert.That(result, Is.Null);
        }

        await using (var db = DbForStudio(_seed.StudioB.StudioId))
        {
            var item = await new InventoryRepository(db).GetByIdAsync(_seed.StudioB.InventoryId);
            Assert.That(item!.Name, Is.EqualTo("Wax"));
        }
    }
}
