namespace Scheduling.Domain.Bookings;

// The booking's own copies of the CRM client and vehicle; CRM stays the source of truth

public sealed record BookingClient(Guid Id, string Name, string Phone, string? Email);

public sealed record BookingVehicle(Guid Id, string LicensePlate, string? Brand, string? Model);
