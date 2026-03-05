using Core.DTOs.Vehicles;
using Core.Interfaces.Mappings;
using Core.Models;
using Riok.Mapperly.Abstractions;

namespace Core.Mappings;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class VehicleMapper : IVehicleMapper
{
    public partial VehicleInfoDto MapVehicleToResponse(Vehicle vehicle);

    [MapperIgnoreTarget(nameof(Vehicle.Id))]
    [MapperIgnoreTarget(nameof(Vehicle.StudioId))]
    [MapperIgnoreTarget(nameof(Vehicle.IsDeleted))]
    [MapperIgnoreTarget(nameof(Vehicle.CreatedAt))]
    [MapperIgnoreTarget(nameof(Vehicle.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Vehicle.Bookings))]
    [MapperIgnoreTarget(nameof(Vehicle.Client))]
    [MapperIgnoreTarget(nameof(Vehicle.Studio))]
    public partial Vehicle MapToEntity(CreateVehicleRequest dto);

    public partial void UpdateVehicle(CreateVehicleRequest dto, Vehicle vehicle);
}