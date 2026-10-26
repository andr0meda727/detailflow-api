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
}
