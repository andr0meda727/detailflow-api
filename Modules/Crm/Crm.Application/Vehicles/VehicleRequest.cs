using Shared.Kernel.Vehicles;

namespace Crm.Application.Vehicles;

// Size is optional so clients that don't send it keep working: new vehicles are Medium, edits keep the current size
public record VehicleRequest(
    Guid ClientId, string LicensePlate, string? Brand, string? Model, VehicleSize? Size = null);
