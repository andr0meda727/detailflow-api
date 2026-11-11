using Crm.Domain;
using Shared.Kernel.Vehicles;

namespace Crm.Application.Vehicles;

public sealed record VehicleResponseDto(Guid Id, string LicensePlate, string? Brand, string? Model, VehicleSize Size)
{
    public static VehicleResponseDto From(Vehicle vehicle) =>
        new(vehicle.Id, vehicle.LicensePlate, vehicle.Brand, vehicle.Model, vehicle.Size);
}
