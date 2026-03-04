using Core.DTOs.Services;
using Core.Interfaces;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Core.Services;

public class ServicesService(
    IServiceRepository serviceRepository,
    ITenantContext tenantContext,
    IServiceMapper mapper) : IServicesService
{
    public async Task<List<ServiceResponseDto>> GetAllAsync()
    {
        var services = await serviceRepository.GetAllActiveAsync();
        return services.Select(mapper.MapServiceToResponse).ToList();
    }

    public async Task<ServiceResponseDto> AddAsync(CreateServiceDto dto)
    {
        var service = mapper.MapToEntity(dto);

        service.Id = Guid.NewGuid();
        service.StudioId = tenantContext.StudioId;
        service.IsActive = true;
        service.CreatedAt = DateTime.UtcNow;
        service.UpdatedAt = DateTime.UtcNow;

        var addedService = await serviceRepository.AddAsync(service);
        return mapper.MapServiceToResponse(addedService);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var service = await serviceRepository.GetByIdAsync(id);

        if (service == null || !service.IsActive)
        {
            return false;
        }

        service.IsActive = false;
        service.UpdatedAt = DateTime.UtcNow;

        await serviceRepository.UpdateAsync(service);
        return true;
    }
}
