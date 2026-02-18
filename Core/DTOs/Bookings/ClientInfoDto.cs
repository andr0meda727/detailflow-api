namespace Core.DTOs.Bookings;

public record ClientInfoDto (
    Guid Id,
    string Name,
    string? Email,
    string? Phone
);