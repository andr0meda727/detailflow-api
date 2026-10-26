namespace Crm.Application.Vehicles;

public record VehicleRequest(Guid ClientId, string LicensePlate, string? Brand, string? Model);
