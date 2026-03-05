using Core.DTOs.Vehicles;
using Core.Models;

namespace Core.Interfaces.Mappings;

public interface IVehicleMapper
{
    VehicleInfoDto MapVehicleToResponse(Vehicle vehicle);
    Vehicle MapToEntity(CreateVehicleRequest dto);
    void UpdateVehicle(CreateVehicleRequest dto, Vehicle vehicle);
}