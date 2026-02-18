using Core.DTOs.Bookings;
using Core.DTOs.Vehicles;

namespace Core.DTOs.Clients;

public record ClientResponseDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? Nip,
    List<VehicleInfoDto> Vehicles
);