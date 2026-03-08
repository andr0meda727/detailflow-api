using Core.DTOs.Clients;
using Core.DTOs.Vehicles;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Models;
using Core.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Unit.Services;

[TestFixture]
public class ClientsServiceTests
{
    private IClientRepository _clientRepository;
    private IClientMapper _clientMapper;
    private IVehicleRepository _vehicleRepository;
    private IVehicleMapper _vehicleMapper;
    private ClientsService _sut;

    [SetUp]
    public void SetUp()
    {
        _clientRepository = Substitute.For<IClientRepository>();
        _clientMapper = Substitute.For<IClientMapper>();
        _vehicleRepository = Substitute.For<IVehicleRepository>();
        _vehicleMapper = Substitute.For<IVehicleMapper>();
        _sut = new ClientsService(_clientRepository, _clientMapper, _vehicleRepository, _vehicleMapper);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetAllClientsAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllClientsAsync_ReturnsAllMappedClients()
    {
        var clients = new List<Client> { BuildClient(), BuildClient() };
        _clientRepository.GetAllWithVehiclesAsync(null).Returns(clients);
        _clientMapper.MapClientToResponse(Arg.Any<Client>())
                     .Returns(ci => BuildClientResponseDto(ci.Arg<Client>().Id));

        var result = await _sut.GetAllClientsAsync(null);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAllClientsAsync_ForwardsSearchTermToRepository()
    {
        _clientRepository.GetAllWithVehiclesAsync("john").Returns(new List<Client>());

        await _sut.GetAllClientsAsync("john");

        await _clientRepository.Received(1).GetAllWithVehiclesAsync("john");
    }

    [Test]
    public async Task GetAllClientsAsync_ReturnsEmptyList_WhenNoClientsFound()
    {
        _clientRepository.GetAllWithVehiclesAsync(Arg.Any<string?>()).Returns(new List<Client>());

        var result = await _sut.GetAllClientsAsync("nonexistent");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAllClientsAsync_MapsEachClient()
    {
        var clients = new List<Client> { BuildClient(), BuildClient(), BuildClient() };
        _clientRepository.GetAllWithVehiclesAsync(null).Returns(clients);
        _clientMapper.MapClientToResponse(Arg.Any<Client>()).Returns(BuildClientResponseDto());

        await _sut.GetAllClientsAsync(null);

        _clientMapper.Received(3).MapClientToResponse(Arg.Any<Client>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetClientByIdAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetClientByIdAsync_ReturnsMappedClient_WhenFound()
    {
        var id = Guid.NewGuid();
        var client = BuildClient(id);
        var expected = BuildClientResponseDto(id);

        _clientRepository.GetClientByIdWithVehiclesAsync(id).Returns(client);
        _clientMapper.MapClientToResponse(client).Returns(expected);

        var result = await _sut.GetClientByIdAsync(id);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetClientByIdAsync_ReturnsNull_WhenNotFound()
    {
        _clientRepository.GetClientByIdWithVehiclesAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.GetClientByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AddClientAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task AddClientAsync_AddsClientAndReturnsDto()
    {
        var dto = new CreateClientDto { Name = "Anna Nowak", Phone = "123456789" };
        var entity = BuildClient();
        var expected = BuildClientResponseDto(entity.Id);

        _clientMapper.MapToEntity(dto).Returns(entity);
        _clientRepository.AddAsync(entity).Returns(entity);
        _clientMapper.MapClientToResponse(entity).Returns(expected);

        var result = await _sut.AddClientAsync(dto);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task AddClientAsync_AssignsNewGuidToEntity()
    {
        var dto = new CreateClientDto { Name = "Test", Phone = "000000000" };
        var entity = BuildClient(Guid.Empty);

        _clientMapper.MapToEntity(dto).Returns(entity);
        _clientRepository.AddAsync(Arg.Any<Client>()).Returns(entity);
        _clientMapper.MapClientToResponse(Arg.Any<Client>()).Returns(BuildClientResponseDto());

        await _sut.AddClientAsync(dto);

        Assert.That(entity.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task AddClientAsync_AddsVehicle_WhenVehicleDtoProvided()
    {
        var vehicleDto = new CreateClientVehicleDto("KR 12345", "BMW", "3 Series");
        var dto = new CreateClientDto { Name = "Test", Phone = "000000000", Vehicle = vehicleDto };
        var entity = BuildClient();
        var vehicleEntity = new Vehicle { Id = Guid.NewGuid(), LicensePlate = "KR 12345", StudioId = Guid.NewGuid() };

        _clientMapper.MapToEntity(dto).Returns(entity);
        _clientRepository.AddAsync(entity).Returns(entity);
        _vehicleMapper.MapToEntity(vehicleDto).Returns(vehicleEntity);
        _vehicleRepository.AddAsync(vehicleEntity).Returns(vehicleEntity);
        _clientMapper.MapClientToResponse(entity).Returns(BuildClientResponseDto());

        await _sut.AddClientAsync(dto);

        await _vehicleRepository.Received(1).AddAsync(vehicleEntity);
    }

    [Test]
    public async Task AddClientAsync_SetsVehicleClientId_WhenVehicleDtoProvided()
    {
        var vehicleDto = new CreateClientVehicleDto("KR 12345", "Toyota", "Corolla");
        var dto = new CreateClientDto { Name = "Test", Phone = "000000000", Vehicle = vehicleDto };
        var clientEntity = BuildClient();
        var vehicleEntity = new Vehicle { LicensePlate = "KR 12345", StudioId = Guid.NewGuid() };

        _clientMapper.MapToEntity(dto).Returns(clientEntity);
        _clientRepository.AddAsync(clientEntity).Returns(clientEntity);
        _vehicleMapper.MapToEntity(vehicleDto).Returns(vehicleEntity);
        _vehicleRepository.AddAsync(vehicleEntity).Returns(vehicleEntity);
        _clientMapper.MapClientToResponse(clientEntity).Returns(BuildClientResponseDto());

        await _sut.AddClientAsync(dto);

        Assert.That(vehicleEntity.ClientId, Is.EqualTo(clientEntity.Id));
    }

    [Test]
    public async Task AddClientAsync_DoesNotAddVehicle_WhenVehicleDtoIsNull()
    {
        var dto = new CreateClientDto { Name = "Test", Phone = "000000000", Vehicle = null };
        var entity = BuildClient();

        _clientMapper.MapToEntity(dto).Returns(entity);
        _clientRepository.AddAsync(entity).Returns(entity);
        _clientMapper.MapClientToResponse(entity).Returns(BuildClientResponseDto());

        await _sut.AddClientAsync(dto);

        await _vehicleRepository.DidNotReceive().AddAsync(Arg.Any<Vehicle>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UpdateClientAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateClientAsync_ReturnsNull_WhenClientNotFound()
    {
        _clientRepository.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.UpdateClientAsync(Guid.NewGuid(), new CreateClientDto());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateClientAsync_CallsMapper_WhenClientFound()
    {
        var id = Guid.NewGuid();
        var client = BuildClient(id);
        var dto = new CreateClientDto { Name = "Updated", Phone = "987654321" };

        _clientRepository.GetByIdAsync(id).Returns(client);
        _clientRepository.GetClientByIdWithVehiclesAsync(id).Returns(client);
        _clientMapper.MapClientToResponse(client).Returns(BuildClientResponseDto(id));

        await _sut.UpdateClientAsync(id, dto);

        _clientMapper.Received(1).UpdateClient(dto, client);
    }

    [Test]
    public async Task UpdateClientAsync_CallsRepositoryUpdate_WhenClientFound()
    {
        var id = Guid.NewGuid();
        var client = BuildClient(id);
        var dto = new CreateClientDto { Name = "Updated", Phone = "987654321" };

        _clientRepository.GetByIdAsync(id).Returns(client);
        _clientRepository.GetClientByIdWithVehiclesAsync(id).Returns(client);
        _clientMapper.MapClientToResponse(client).Returns(BuildClientResponseDto(id));

        await _sut.UpdateClientAsync(id, dto);

        await _clientRepository.Received(1).UpdateAsync(client);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DeleteClientAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteClientAsync_ReturnsTrue_WhenClientExists()
    {
        var id = Guid.NewGuid();
        _clientRepository.GetByIdAsync(id).Returns(BuildClient(id));

        var result = await _sut.DeleteClientAsync(id);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeleteClientAsync_ReturnsFalse_WhenClientNotFound()
    {
        _clientRepository.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.DeleteClientAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteClientAsync_SetsIsDeletedToTrue_WhenClientExists()
    {
        var id = Guid.NewGuid();
        var client = BuildClient(id);
        client.IsDeleted = false;
        _clientRepository.GetByIdAsync(id).Returns(client);

        await _sut.DeleteClientAsync(id);

        Assert.That(client.IsDeleted, Is.True);
    }

    [Test]
    public async Task DeleteClientAsync_CallsUpdateAsync_WhenClientExists()
    {
        var id = Guid.NewGuid();
        var client = BuildClient(id);
        _clientRepository.GetByIdAsync(id).Returns(client);

        await _sut.DeleteClientAsync(id);

        await _clientRepository.Received(1).UpdateAsync(client);
    }

    [Test]
    public async Task DeleteClientAsync_DoesNotCallUpdate_WhenClientNotFound()
    {
        _clientRepository.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        await _sut.DeleteClientAsync(Guid.NewGuid());

        await _clientRepository.DidNotReceive().UpdateAsync(Arg.Any<Client>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static Client BuildClient(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Test Client",
        Email = "test@example.com",
        Phone = "123456789",
        StudioId = Guid.NewGuid(),
        IsDeleted = false,
        Vehicles = new List<Vehicle>()
    };

    private static ClientResponseDto BuildClientResponseDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "Test Client", "test@example.com", "123456789", null,
            new List<VehicleInfoDto>());
}
