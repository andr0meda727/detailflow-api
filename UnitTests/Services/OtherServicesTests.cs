using Core.DTOs.Services;
using Core.DTOs.Vehicles;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Models;
using Core.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Shared.Kernel.Pagination;

namespace Unit.Services;

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
