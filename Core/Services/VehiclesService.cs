using Core.DTOs.Vehicles;
using Shared.Kernel.Tenancy;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Shared.Kernel.Pagination;

namespace Core.Services;

public class VehiclesService(
    IVehicleRepository vehicleRepository,
    IClientRepository clientRepository,
    IVehicleMapper mapper) : IVehiclesService
{
    public async Task<PagedResponse<VehicleInfoDto>> GetAllAsync(PageRequest page, Guid? clientId = null, string? search = null)
    {
        var vehicles = await vehicleRepository.GetAllFilteredAsync(page, clientId, search);
        return vehicles.Map(mapper.MapVehicleToResponse);
    }

    public async Task<VehicleInfoDto?> AddAsync(CreateVehicleRequest dto)
    {
        var client = await clientRepository.GetByIdAsync(dto.ClientId);
        if (client == null)
        {
            return null;
        }

        var vehicle = mapper.MapToEntity(dto);

        var addedVehicle = await vehicleRepository.AddAsync(vehicle);
        return mapper.MapVehicleToResponse(addedVehicle);
    }

    public async Task<VehicleInfoDto?> UpdateVehicleAsync(Guid id, CreateVehicleRequest dto)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(id);

        if (vehicle == null) return null;

        mapper.UpdateVehicle(dto, vehicle);

        await vehicleRepository.UpdateAsync(vehicle);

        return mapper.MapVehicleToResponse(vehicle);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(id);

        if (vehicle == null)
        {
            return false;
        }

        vehicle.IsDeleted = true;

        await vehicleRepository.UpdateAsync(vehicle);
        return true;
    }
}