using Core.DTOs.Services;

namespace Core.Interfaces.Services;

public interface IServicesService
{
    Task<List<ServiceResponseDto>> GetAllAsync();
    Task<ServiceResponseDto> AddAsync(CreateServiceDto dto);
    Task<bool> DeleteAsync(Guid id);
}
