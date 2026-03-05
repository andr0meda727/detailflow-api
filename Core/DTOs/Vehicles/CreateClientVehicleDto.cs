namespace Core.DTOs.Vehicles;

// Used for POST /api/clients, so we can create a vehicle for a client at the same time
public record CreateClientVehicleDto(string LicensePlate, string? Brand, string? Model);