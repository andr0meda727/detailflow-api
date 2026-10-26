using Core.DTOs.Vehicles;
using Shared.Kernel.Pagination;

namespace Core.Interfaces.Services;

public interface IVehiclesService
{
    Task<PagedResponse<VehicleInfoDto>> GetAllAsync(PageRequest page, Guid? clientId = null, string? search = null);
    Task<VehicleInfoDto?> AddAsync(CreateVehicleRequest dto);
    Task<VehicleInfoDto?> UpdateVehicleAsync(Guid id, CreateVehicleRequest dto);

    Task<bool> DeleteAsync(Guid id);
}