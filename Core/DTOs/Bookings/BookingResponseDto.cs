using Core.DTOs.Vehicles;

namespace Core.DTOs.Bookings;

public record BookingResponseDto(
    Guid Id,
    DateTime ScheduledAt,
    DateTime? CompletedAt,
    decimal TotalPrice,
    string Status,
    string StatusColor,
    string? Notes,
    VehicleInfoDto Vehicle,
    ClientInfoDto Client,
    List<string> Services
);