using Core.DTOs.Services;
using Core.DTOs.Vehicles;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Models;
using Core.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Unit.Services;

// ════════════════════════════════════════════════════════════════════════════
// VehiclesService
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class VehiclesServiceTests
{
    private IVehicleRepository _vehicleRepository;
    private IClientRepository _clientRepository;
    private IVehicleMapper _vehicleMapper;
    private VehiclesService _sut;

    [SetUp]
    public void SetUp()
    {
        _vehicleRepository = Substitute.For<IVehicleRepository>();
        _clientRepository = Substitute.For<IClientRepository>();
        _vehicleMapper = Substitute.For<IVehicleMapper>();
        _sut = new VehiclesService(_vehicleRepository, _clientRepository, _vehicleMapper);
    }

    [Test]
    public async Task GetAllAsync_ReturnsMappedVehicles()
    {
        var vehicles = new List<Vehicle> { BuildVehicle(), BuildVehicle() };
        _vehicleRepository.GetAllFilteredAsync(null, null).Returns(vehicles);
        _vehicleMapper.MapVehicleToResponse(Arg.Any<Vehicle>())
                      .Returns(ci => BuildVehicleInfoDto(ci.Arg<Vehicle>().Id));

        var result = await _sut.GetAllAsync();

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAllAsync_ForwardsClientIdAndSearchToRepository()
    {
        var clientId = Guid.NewGuid();
        _vehicleRepository.GetAllFilteredAsync(clientId, "BMW").Returns(new List<Vehicle>());

        await _sut.GetAllAsync(clientId, "BMW");

        await _vehicleRepository.Received(1).GetAllFilteredAsync(clientId, "BMW");
    }

    [Test]
    public async Task AddAsync_ReturnsNull_WhenClientNotFound()
    {
        var dto = new CreateVehicleRequest(Guid.NewGuid(), "KR 12345", "BMW", "3");
        _clientRepository.GetByIdAsync(dto.ClientId).ReturnsNull();

        var result = await _sut.AddAsync(dto);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AddAsync_AddsAndReturnsVehicle_WhenClientExists()
    {
        var clientId = Guid.NewGuid();
        var dto = new CreateVehicleRequest(clientId, "KR 12345", "BMW", "3");
        var vehicle = BuildVehicle();
        var expected = BuildVehicleInfoDto(vehicle.Id);

        _clientRepository.GetByIdAsync(clientId).Returns(new Client { Id = clientId, Phone = "000", StudioId = Guid.NewGuid() });
        _vehicleMapper.MapToEntity(dto).Returns(vehicle);
        _vehicleRepository.AddAsync(vehicle).Returns(vehicle);
        _vehicleMapper.MapVehicleToResponse(vehicle).Returns(expected);

        var result = await _sut.AddAsync(dto);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task AddAsync_DoesNotCallRepository_WhenClientNotFound()
    {
        var dto = new CreateVehicleRequest(Guid.NewGuid(), "KR 12345", "BMW", "3");
        _clientRepository.GetByIdAsync(dto.ClientId).ReturnsNull();

        await _sut.AddAsync(dto);

        await _vehicleRepository.DidNotReceive().AddAsync(Arg.Any<Vehicle>());
    }

    [Test]
    public async Task UpdateVehicleAsync_ReturnsNull_WhenVehicleNotFound()
    {
        _vehicleRepository.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.UpdateVehicleAsync(Guid.NewGuid(),
            new CreateVehicleRequest(Guid.NewGuid(), "KR 00000", null, null));

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateVehicleAsync_CallsMapper_WhenVehicleFound()
    {
        var id = Guid.NewGuid();
        var vehicle = BuildVehicle(id);
        var dto = new CreateVehicleRequest(Guid.NewGuid(), "KR 99999", "Audi", "A4");

        _vehicleRepository.GetByIdAsync(id).Returns(vehicle);
        _vehicleMapper.MapVehicleToResponse(vehicle).Returns(BuildVehicleInfoDto(id));

        await _sut.UpdateVehicleAsync(id, dto);

        _vehicleMapper.Received(1).UpdateVehicle(dto, vehicle);
    }

    [Test]
    public async Task UpdateVehicleAsync_CallsUpdateOnRepository()
    {
        var id = Guid.NewGuid();
        var vehicle = BuildVehicle(id);
        var dto = new CreateVehicleRequest(Guid.NewGuid(), "KR 99999", "Audi", "A4");

        _vehicleRepository.GetByIdAsync(id).Returns(vehicle);
        _vehicleMapper.MapVehicleToResponse(vehicle).Returns(BuildVehicleInfoDto(id));

        await _sut.UpdateVehicleAsync(id, dto);

        await _vehicleRepository.Received(1).UpdateAsync(vehicle);
    }

    [Test]
    public async Task DeleteAsync_ReturnsTrue_WhenVehicleExists()
    {
        var id = Guid.NewGuid();
        _vehicleRepository.GetByIdAsync(id).Returns(BuildVehicle(id));

        var result = await _sut.DeleteAsync(id);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeleteAsync_ReturnsFalse_WhenVehicleNotFound()
    {
        _vehicleRepository.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteAsync_SetsIsDeletedTrue_WhenVehicleExists()
    {
        var id = Guid.NewGuid();
        var vehicle = BuildVehicle(id);
        vehicle.IsDeleted = false;
        _vehicleRepository.GetByIdAsync(id).Returns(vehicle);

        await _sut.DeleteAsync(id);

        Assert.That(vehicle.IsDeleted, Is.True);
        await _vehicleRepository.Received(1).UpdateAsync(vehicle);
    }

    [Test]
    public async Task DeleteAsync_DoesNotCallUpdate_WhenVehicleNotFound()
    {
        _vehicleRepository.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        await _sut.DeleteAsync(Guid.NewGuid());

        await _vehicleRepository.DidNotReceive().UpdateAsync(Arg.Any<Vehicle>());
    }

    private static Vehicle BuildVehicle(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        LicensePlate = "KR 12345",
        Brand = "BMW",
        Model = "3",
        StudioId = Guid.NewGuid(),
        ClientId = Guid.NewGuid()
    };

    private static VehicleInfoDto BuildVehicleInfoDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "KR 12345", "BMW", "3");
}

// ════════════════════════════════════════════════════════════════════════════
// ServicesService
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class ServicesServiceTests
{
    private IServiceRepository _serviceRepository;
    private IServiceMapper _serviceMapper;
    private ServicesService _sut;

    [SetUp]
    public void SetUp()
    {
        _serviceRepository = Substitute.For<IServiceRepository>();
        _serviceMapper = Substitute.For<IServiceMapper>();
        _sut = new ServicesService(_serviceRepository, _serviceMapper);
    }

    [Test]
    public async Task GetAllAsync_ReturnsMappedActiveServices()
    {
        var services = new List<Service> { BuildService(), BuildService() };
        _serviceRepository.GetAllActiveAsync().Returns(services);
        _serviceMapper.MapServiceToResponse(Arg.Any<Service>())
                      .Returns(ci => new ServiceResponseDto(ci.Arg<Service>().Id, "Test"));

        var result = await _sut.GetAllAsync();

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoActiveServices()
    {
        _serviceRepository.GetAllActiveAsync().Returns(new List<Service>());

        var result = await _sut.GetAllAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task AddAsync_AddsAndReturnsMappedService()
    {
        var dto = new CreateServiceDto("Mycie zewnętrzne");
        var entity = BuildService();
        var expected = new ServiceResponseDto(entity.Id, dto.Name);

        _serviceMapper.MapToEntity(dto).Returns(entity);
        _serviceRepository.AddAsync(entity).Returns(entity);
        _serviceMapper.MapServiceToResponse(entity).Returns(expected);

        var result = await _sut.AddAsync(dto);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task AddAsync_CallsRepositoryAdd()
    {
        var dto = new CreateServiceDto("Korekta lakieru");
        var entity = BuildService();

        _serviceMapper.MapToEntity(dto).Returns(entity);
        _serviceRepository.AddAsync(entity).Returns(entity);
        _serviceMapper.MapServiceToResponse(entity).Returns(new ServiceResponseDto(entity.Id, dto.Name));

        await _sut.AddAsync(dto);

        await _serviceRepository.Received(1).AddAsync(entity);
    }

    [Test]
    public async Task DeleteAsync_ReturnsTrue_AndSetsInactive_WhenServiceExists()
    {
        var id = Guid.NewGuid();
        var service = BuildService(id);
        service.IsActive = true;

        _serviceRepository.GetByIdAsync(id).Returns(service);

        var result = await _sut.DeleteAsync(id);

        Assert.That(result, Is.True);
        Assert.That(service.IsActive, Is.False);
        await _serviceRepository.Received(1).UpdateAsync(service);
    }

    [Test]
    public async Task DeleteAsync_ReturnsFalse_WhenServiceNotFound()
    {
        _serviceRepository.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteAsync_DoesNotCallUpdate_WhenServiceNotFound()
    {
        _serviceRepository.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        await _sut.DeleteAsync(Guid.NewGuid());

        await _serviceRepository.DidNotReceive().UpdateAsync(Arg.Any<Service>());
    }

    private static Service BuildService(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Test Service",
        IsActive = true,
        StudioId = Guid.NewGuid()
    };
}
