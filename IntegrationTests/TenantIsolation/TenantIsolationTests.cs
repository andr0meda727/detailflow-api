using Core.Models;
using Core.Services;
using Infrastructure.Data;
using Infrastructure.Repositories;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.TenantIsolation;

/// <summary>
/// Every tenant-scoped read must return only the current studio's rows,
/// and nothing at all when no studio is resolved (fail closed).
/// </summary>
[TestFixture]
public class TenantIsolationTests
{
    private TwoStudiosSeed _seed = null!;

    [OneTimeSetUp]
    public async Task SeedAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
    }

    private ApplicationDbContext DbForStudio(Guid studioId)
    {
        var tenant = new TenantContext();
        tenant.SetTenant(studioId, Guid.NewGuid());
        return PostgresFixture.CreateDbContext(tenant);
    }

    private static ApplicationDbContext DbForUserWithoutStudio()
    {
        var tenant = new TenantContext();
        tenant.SetUserOnly(Guid.NewGuid());
        return PostgresFixture.CreateDbContext(tenant);
    }

    [Test]
    public async Task Bookings_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var bookings = await new BookingRepository(db).GetAllWithDetailsAsync();

        Assert.That(bookings.Select(b => b.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
        Assert.That(bookings.Select(b => b.Id), Does.Contain(_seed.StudioA.BookingId));
    }

    [Test]
    public async Task Clients_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var clients = await new ClientRepository(db).GetAllWithVehiclesAsync(null);

        Assert.That(clients.Select(c => c.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
        Assert.That(clients.Select(c => c.Id), Does.Contain(_seed.StudioA.ClientId));
    }

    [Test]
    public async Task Vehicles_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var vehicles = await new VehicleRepository(db).GetAllFilteredAsync();

        Assert.That(vehicles.Select(v => v.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
    }

    [Test]
    public async Task Services_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var services = await new ServiceRepository(db).GetAllActiveAsync();

        Assert.That(services.Select(s => s.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
    }

    [Test]
    public async Task InventoryCategories_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var categories = await new InventoryCategoryRepository(db).GetAllAsync();

        Assert.That(categories.Select(c => c.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
    }

    [Test]
    public async Task Inventory_IsScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);
        var repository = new InventoryRepository(db);

        var all = await repository.GetAllAsync();
        var lowStock = await repository.GetLowStockAsync();

        Assert.That(all.Select(i => i.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
        Assert.That(lowStock.Select(i => i.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
    }

    [Test]
    public async Task GetById_DoesNotReturnOtherStudiosRecord()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        Assert.Multiple(async () =>
        {
            Assert.That(await new BookingRepository(db).GetByIdWithDetailsAsync(_seed.StudioB.BookingId), Is.Null);
            Assert.That(await new ClientRepository(db).GetClientByIdWithVehiclesAsync(_seed.StudioB.ClientId), Is.Null);
            Assert.That(await new InventoryRepository(db).GetByIdAsync(_seed.StudioB.InventoryId), Is.Null);
        });
    }

    [Test]
    public async Task UserWithoutStudio_SeesNoTenantData()
    {
        await using var db = DbForUserWithoutStudio();

        Assert.Multiple(async () =>
        {
            Assert.That(await new BookingRepository(db).GetAllWithDetailsAsync(), Is.Empty);
            Assert.That(await new ClientRepository(db).GetAllWithVehiclesAsync(null), Is.Empty);
            Assert.That(await new VehicleRepository(db).GetAllFilteredAsync(), Is.Empty);
            Assert.That(await new ServiceRepository(db).GetAllActiveAsync(), Is.Empty);
            Assert.That(await new InventoryCategoryRepository(db).GetAllAsync(), Is.Empty);
            Assert.That(await new InventoryRepository(db).GetAllAsync(), Is.Empty);
        });
    }

    [Test]
    public async Task ServiceLookup_DoesNotCountOtherStudiosServices()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);
        var ids = new[] { _seed.StudioA.ServiceId, _seed.StudioB.ServiceId };

        var count = await new ServiceRepository(db).CountExistingAsync(ids);

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task UserWithoutStudio_CannotAddTenantData()
    {
        await using var db = DbForUserWithoutStudio();

        db.Clients.Add(new Client { Name = "Anna", Phone = "500600700" });

        Assert.That(() => db.SaveChangesAsync(), Throws.InvalidOperationException);
    }
}
