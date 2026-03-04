using Core.DTOs.Vehicles;

namespace Core.Interfaces.Services;

public interface IVehiclesService
{
    Task<List<VehicleInfoDto>> GetAllAsync(Guid? clientId = null, string? search = null);
    Task<VehicleInfoDto?> AddAsync(CreateVehicleRequest dto);
    Task<bool> DeleteAsync(Guid id);
}
