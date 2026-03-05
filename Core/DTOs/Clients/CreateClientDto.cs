using Core.DTOs.Vehicles;

namespace Core.DTOs.Clients;

public class CreateClientDto
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Nip { get; set; }

    public CreateClientVehicleDto? Vehicle { get; set; }
}