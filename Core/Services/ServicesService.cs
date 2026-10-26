using Core.DTOs.Services;
using Shared.Kernel.Tenancy;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Core.Services;

public class ServicesService(
    IServiceRepository serviceRepository,
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

        var addedService = await serviceRepository.AddAsync(service);
        return mapper.MapServiceToResponse(addedService);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var service = await serviceRepository.GetByIdAsync(id);

        if (service == null)
        {
            return false;
        }

        service.IsActive = false;

        await serviceRepository.UpdateAsync(service);
        return true;
    }
}