namespace Core.DTOs.Vehicles;

public record VehicleInfoDto(
    Guid Id,
    string LicensePlate,
    string? Brand,
    string? Model
);