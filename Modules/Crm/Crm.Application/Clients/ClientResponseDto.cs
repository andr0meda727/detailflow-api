using Crm.Application.Vehicles;
using Crm.Domain;

namespace Crm.Application.Clients;

public sealed record ClientResponseDto(
    Guid Id,
    string Name,
    string? Email,
    string Phone,
    string? Nip,
    List<VehicleResponseDto> Vehicles)
{
    // Expects Vehicles to be loaded; deleted vehicles are filtered out by the context
    public static ClientResponseDto From(Client client) =>
        new(client.Id, client.Name, client.Email, client.Phone, client.Nip,
            client.Vehicles.OrderBy(v => v.LicensePlate).Select(VehicleResponseDto.From).ToList());
}
