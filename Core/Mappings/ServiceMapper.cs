using Core.DTOs.Services;
using Core.Interfaces.Mappings;
using Core.Models;
using Riok.Mapperly.Abstractions;

namespace Core.Mappings;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ServiceMapper : IServiceMapper
{
    public partial ServiceResponseDto MapServiceToResponse(Service service);

    [MapperIgnoreTarget(nameof(Service.Id))]
    [MapperIgnoreTarget(nameof(Service.StudioId))]
    [MapperIgnoreTarget(nameof(Service.IsActive))]
    [MapperIgnoreTarget(nameof(Service.CreatedAt))]
    [MapperIgnoreTarget(nameof(Service.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Service.BookingServices))]
    [MapperIgnoreTarget(nameof(Service.Studio))]
    public partial Service MapToEntity(CreateServiceDto dto);
}
