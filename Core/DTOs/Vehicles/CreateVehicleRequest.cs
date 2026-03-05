namespace Core.DTOs.Vehicles;

public record CreateVehicleRequest(
    Guid ClientId,
    string LicensePlate,
    string? Brand,
    string? Model
);
