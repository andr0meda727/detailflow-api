using Crm.Application.Clients;
using Crm.Application.Clients.CreateClient;
using Crm.Application.Clients.DeleteClient;
using Crm.Application.Clients.GetClient;
using Crm.Application.Clients.GetClients;
using Crm.Application.Clients.UpdateClient;
using Crm.Application.Directory;
using Crm.Application.Vehicles;
using Crm.Application.Vehicles.CreateVehicle;
using Crm.Application.Vehicles.DeleteVehicle;
using Crm.Application.Vehicles.GetVehicles;
using IntegrationTests.Infrastructure;
using Shared.Kernel.Pagination;
using static IntegrationTests.Modules.Crm.CrmTenantIsolationTests;

namespace IntegrationTests.Modules.Crm;

[TestFixture]
public class CrmHandlerTests
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

    private static string NewPhone() => Random.Shared.Next(100_000_000, 999_999_999).ToString();

    [Test]
    public async Task CreateClient_WithVehicle_SavesBoth()
    {
        await using var db = DbForStudio(StudioId);

        var created = await new CreateClientHandler(db).HandleAsync(new ClientRequest
        {
            Name = "Anna Nowak", Phone = NewPhone(), Vehicle = new ClientVehicleRequest("kr 777", "Audi", "A4")
        }, Ct);

        await using var readDb = DbForStudio(StudioId);
        var reloaded = await new GetClientHandler(readDb).HandleAsync(created.Value!.Id, Ct);
        Assert.That(reloaded.Value!.Name, Is.EqualTo("Anna Nowak"));
        Assert.That(reloaded.Value.Vehicles.Single().LicensePlate, Is.EqualTo("KR 777"));
    }

    [Test]
    public async Task CreateClient_WithPhoneOfAnotherClient_IsAConflict()
    {
        await using var db = DbForStudio(StudioId);

        var result = await new CreateClientHandler(db).HandleAsync(
            new ClientRequest { Name = "Duplikat", Phone = _seed.SharedClientPhone }, Ct);

        Assert.That(result.ErrorCode, Is.EqualTo("CLIENT_PHONE_TAKEN"));
    }

    [Test]
    public async Task DeletedClient_FreesPhoneAndDisappears()
    {
        await using var db = DbForStudio(StudioId);
        var phone = NewPhone();
        var client = await new CreateClientHandler(db).HandleAsync(new ClientRequest { Name = "Stary", Phone = phone }, Ct);

        await new DeleteClientHandler(db).HandleAsync(client.Value!.Id, Ct);
        var again = await new CreateClientHandler(db).HandleAsync(new ClientRequest { Name = "Nowy", Phone = phone }, Ct);

        Assert.That(again.Success, Is.True);
        Assert.That((await new GetClientHandler(db).HandleAsync(client.Value.Id, Ct)).ErrorCode, Is.EqualTo("CLIENT_NOT_FOUND"));
    }

    [Test]
    public async Task UpdateClient_KeepsOwnPhone()
    {
        await using var db = DbForStudio(StudioId);
        var phone = NewPhone();
        var client = await new CreateClientHandler(db).HandleAsync(new ClientRequest { Name = "Jan", Phone = phone }, Ct);

        var updated = await new UpdateClientHandler(db).HandleAsync(client.Value!.Id,
            new ClientRequest { Name = "Jan Kowalski", Phone = phone, Email = "jan@test.pl" }, Ct);

        Assert.That(updated.Value!.Name, Is.EqualTo("Jan Kowalski"));
        Assert.That(updated.Value.Email, Is.EqualTo("jan@test.pl"));
    }

    [Test]
    public async Task GetClients_SearchesVehiclePlatesCaseInsensitively()
    {
        await using var db = DbForStudio(StudioId);
        var token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var client = await new CreateClientHandler(db).HandleAsync(new ClientRequest
        {
            Name = "Szukany", Phone = NewPhone(), Vehicle = new ClientVehicleRequest($"PO {token}", null, null)
        }, Ct);

        var found = await new GetClientsHandler(db).HandleAsync(new PageRequest(), token.ToLowerInvariant(), Ct);

        Assert.That(found.Items.Select(c => c.Id), Is.EqualTo(new[] { client.Value!.Id }));
    }

    [Test]
    public async Task Vehicles_CanBeAddedFilteredAndDeleted()
    {
        await using var db = DbForStudio(StudioId);
        var client = await new CreateClientHandler(db).HandleAsync(new ClientRequest { Name = "Flota", Phone = NewPhone() }, Ct);
        var clientId = client.Value!.Id;

        var first = await new CreateVehicleHandler(db).HandleAsync(new VehicleRequest(clientId, "GD 1", "Skoda", null), Ct);
        await new CreateVehicleHandler(db).HandleAsync(new VehicleRequest(clientId, "GD 2", "Skoda", null), Ct);
        await new DeleteVehicleHandler(db).HandleAsync(first.Value!.Id, Ct);

        await using var readDb = DbForStudio(StudioId);
        var vehicles = await new GetVehiclesHandler(readDb).HandleAsync(new PageRequest(), clientId, null, Ct);
        Assert.That(vehicles.Items.Select(v => v.LicensePlate), Is.EqualTo(new[] { "GD 2" }));
    }

    [Test]
    public async Task Directory_IgnoresDeletedVehicles()
    {
        await using var db = DbForStudio(StudioId);
        var client = await new CreateClientHandler(db).HandleAsync(new ClientRequest
        {
            Name = "Bez auta", Phone = NewPhone(), Vehicle = new ClientVehicleRequest("WX 1", null, null)
        }, Ct);
        var vehicleId = client.Value!.Vehicles.Single().Id;
        await new DeleteVehicleHandler(db).HandleAsync(vehicleId, Ct);

        var parties = await new ClientDirectory(db).FindBookingPartiesAsync(client.Value.Id, vehicleId, Ct);

        Assert.That(parties, Is.Null);
    }
}
