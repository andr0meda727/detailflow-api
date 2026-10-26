using Crm.Application.Clients.GetClient;
using Crm.Application.Clients.GetClients;
using Crm.Application.Directory;
using Crm.Application.Vehicles;
using Crm.Application.Vehicles.GetVehicles;
using Crm.Application.Vehicles.UpdateVehicle;
using Crm.Infrastructure.Persistence;
using IntegrationTests.Infrastructure;
using MassTransit;
using NSubstitute;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Pagination;

namespace IntegrationTests.Modules.Crm;

[TestFixture]
public class CrmTenantIsolationTests
{
    private TwoStudiosSeed _seed = null!;
    private static CancellationToken Ct => CancellationToken.None;

    [OneTimeSetUp]
    public async Task SeedAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
    }

    internal static CrmDbContext DbForStudio(Guid studioId)
    {
        var tenant = new TenantContext();
        tenant.SetTenant(studioId, Guid.NewGuid());
        return PostgresFixture.CreateCrmDbContext(tenant);
    }

    private static CrmDbContext DbForUserWithoutStudio()
    {
        var tenant = new TenantContext();
        tenant.SetUserOnly(Guid.NewGuid());
        return PostgresFixture.CreateCrmDbContext(tenant);
    }

    [Test]
    public async Task Clients_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var clients = await new GetClientsHandler(db).HandleAsync(new PageRequest(), null, Ct);

        Assert.That(clients.Items.Select(c => c.Id), Does.Contain(_seed.StudioA.ClientId));
        Assert.That(clients.Items.Select(c => c.Id), Does.Not.Contain(_seed.StudioB.ClientId));
    }

    [Test]
    public async Task Vehicles_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var vehicles = await new GetVehiclesHandler(db).HandleAsync(new PageRequest(), null, null, Ct);

        Assert.That(vehicles.Items.Select(v => v.Id), Does.Contain(_seed.StudioA.VehicleId));
        Assert.That(vehicles.Items.Select(v => v.Id), Does.Not.Contain(_seed.StudioB.VehicleId));
    }

    [Test]
    public async Task GetClient_DoesNotReturnOtherStudiosClient()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var result = await new GetClientHandler(db).HandleAsync(_seed.StudioB.ClientId, Ct);

        Assert.That(result.ErrorCode, Is.EqualTo("CLIENT_NOT_FOUND"));
    }

    [Test]
    public async Task UpdateVehicle_CannotMoveVehicleToOtherStudiosClient()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var result = await new UpdateVehicleHandler(db, Substitute.For<IPublishEndpoint>()).HandleAsync(_seed.StudioA.VehicleId,
            new VehicleRequest(_seed.StudioB.ClientId, "WA12345", null, null), Ct);

        Assert.That(result.ErrorCode, Is.EqualTo("CLIENT_NOT_FOUND"));
    }

    [Test]
    public async Task UserWithoutStudio_SeesNoClientsOrVehicles()
    {
        await using var db = DbForUserWithoutStudio();

        Assert.That((await new GetClientsHandler(db).HandleAsync(new PageRequest(), null, Ct)).Items, Is.Empty);
        Assert.That((await new GetVehiclesHandler(db).HandleAsync(new PageRequest(), null, null, Ct)).Items, Is.Empty);
    }

    [Test]
    public async Task SamePhone_CanBeClientOfDifferentStudios()
    {
        await using var dbA = DbForStudio(_seed.StudioA.StudioId);
        await using var dbB = DbForStudio(_seed.StudioB.StudioId);

        var clientA = await new GetClientHandler(dbA).HandleAsync(_seed.StudioA.ClientId, Ct);
        var clientB = await new GetClientHandler(dbB).HandleAsync(_seed.StudioB.ClientId, Ct);

        Assert.That(clientA.Value!.Phone, Is.EqualTo(clientB.Value!.Phone));
    }

    [Test]
    public async Task Directory_FindsOnlyOwnStudiosClientAndVehicle()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);
        var directory = new ClientDirectory(db);

        var own = await directory.FindBookingPartiesAsync(_seed.StudioA.ClientId, _seed.StudioA.VehicleId, Ct);
        var other = await directory.FindBookingPartiesAsync(_seed.StudioB.ClientId, _seed.StudioB.VehicleId, Ct);
        var mismatched = await directory.FindBookingPartiesAsync(Guid.NewGuid(), _seed.StudioA.VehicleId, Ct);

        Assert.That(own!.Client.Name, Is.EqualTo("Jan"));
        Assert.That(own.Vehicle.LicensePlate, Is.EqualTo("WA12345"));
        Assert.That(other, Is.Null);
        Assert.That(mismatched, Is.Null);
    }
}
