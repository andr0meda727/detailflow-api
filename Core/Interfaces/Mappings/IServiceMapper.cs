using Core.DTOs.Services;
using Core.Models;

namespace Core.Interfaces.Mappings;

public interface IServiceMapper
{
    ServiceResponseDto MapServiceToResponse(Service service);
    Service MapToEntity(CreateServiceDto dto);
}
