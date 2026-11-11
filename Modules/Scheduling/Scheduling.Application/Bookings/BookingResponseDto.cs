using Scheduling.Domain.Bookings;
using Shared.Kernel.Vehicles;

namespace Scheduling.Application.Bookings;

public sealed record BookingResponseDto(
    Guid Id,
    DateTime ScheduledAt,
    DateTime? CompletedAt,
    decimal? TotalPrice,
    string Status,
    string? StatusColor,
    string? Notes,
    BookingVehicleDto Vehicle,
    BookingClientDto Client,
    List<string> Services)
{
    // Expects Status and Services.Service to be loaded (see BookingQueries.WithDetails)
    public static BookingResponseDto From(Booking booking) => new(
        booking.Id,
        booking.ScheduledAt,
        booking.CompletedAt,
        booking.TotalPrice / 100m,
        booking.Status.Name,
        booking.Status.Color,
        booking.Notes,
        new BookingVehicleDto(
            booking.VehicleId, booking.VehicleLicensePlate, booking.VehicleBrand, booking.VehicleModel, booking.VehicleSize),
        new BookingClientDto(booking.ClientId, booking.ClientName, booking.ClientEmail, booking.ClientPhone),
        booking.Services.Select(line => line.Service.Name).Order().ToList());
}

public sealed record BookingVehicleDto(Guid Id, string LicensePlate, string? Brand, string? Model, VehicleSize Size);

public sealed record BookingClientDto(Guid Id, string Name, string? Email, string? Phone);
