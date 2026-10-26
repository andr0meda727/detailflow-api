using Infrastructure.Crm;
using Infrastructure.Data;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.TenantIsolation;

[TestFixture]
public class ClientDirectoryTests
{
    private TwoStudiosSeed _seed = null!;

    [OneTimeSetUp]
    public async Task SeedAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
    }

    private static ApplicationDbContext DbForStudio(Guid studioId)
    {
        var tenant = new TenantContext();
        tenant.SetTenant(studioId, Guid.NewGuid());
        return PostgresFixture.CreateDbContext(tenant);
    }

    [Test]
    public async Task FindsOwnClientAndVehicle()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var parties = await new LegacyClientDirectory(db).FindBookingPartiesAsync(_seed.StudioA.ClientId, _seed.StudioA.VehicleId);

        Assert.That(parties!.Client.Name, Is.EqualTo("Jan"));
        Assert.That(parties.Vehicle.LicensePlate, Is.EqualTo("WA12345"));
    }

    [Test]
    public async Task DoesNotFindOtherStudiosClientAndVehicle()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var parties = await new LegacyClientDirectory(db).FindBookingPartiesAsync(_seed.StudioB.ClientId, _seed.StudioB.VehicleId);

        Assert.That(parties, Is.Null);
    }

    [Test]
    public async Task DoesNotPairVehicleWithAnotherClient()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var parties = await new LegacyClientDirectory(db).FindBookingPartiesAsync(Guid.NewGuid(), _seed.StudioA.VehicleId);

        Assert.That(parties, Is.Null);
    }
}
