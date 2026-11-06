using Shared.Kernel.Vehicles;

namespace Crm.Application.Clients;

public class ClientRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Nip { get; set; }

    // Only used on create, so a client and their first vehicle can be added in one step
    public ClientVehicleRequest? Vehicle { get; set; }
}

public record ClientVehicleRequest(string LicensePlate, string? Brand, string? Model, VehicleSize? Size = null);
