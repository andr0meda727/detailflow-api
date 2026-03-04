using Core.DTOs.Vehicles;
using Core.Interfaces;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Core.Services;

public class VehiclesService(
    IVehicleRepository vehicleRepository,
    IClientRepository clientRepository,
    ITenantContext tenantContext,
    IVehicleMapper mapper) : IVehiclesService
{
    public async Task<List<VehicleInfoDto>> GetAllAsync(Guid? clientId = null, string? search = null)
    {
        var vehicles = await vehicleRepository.GetAllFilteredAsync(clientId, search);
        return vehicles.Select(mapper.MapVehicleToResponse).ToList();
    }

    public async Task<VehicleInfoDto?> AddAsync(CreateVehicleRequest dto)
    {
        var client = await clientRepository.GetByIdAsync(dto.ClientId);
        if (client == null)
        {
            return null;
        }

        var vehicle = mapper.MapToEntity(dto);

        vehicle.Id = Guid.NewGuid();
        vehicle.StudioId = tenantContext.StudioId;
        vehicle.IsDeleted = false;
        vehicle.CreatedAt = DateTime.UtcNow;
        vehicle.UpdatedAt = DateTime.UtcNow;

        var addedVehicle = await vehicleRepository.AddAsync(vehicle);
        return mapper.MapVehicleToResponse(addedVehicle);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(id);

        if (vehicle == null || vehicle.IsDeleted)
        {
            return false;
        }

        vehicle.IsDeleted = true;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await vehicleRepository.UpdateAsync(vehicle);
        return true;
    }
}
