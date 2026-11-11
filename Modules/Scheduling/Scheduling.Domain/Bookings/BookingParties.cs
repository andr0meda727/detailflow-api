using Shared.Kernel.Vehicles;

namespace Scheduling.Domain.Bookings;

// The booking's own copies of the CRM client and vehicle; CRM stays the source of truth

public sealed record BookingClient(Guid Id, string Name, string Phone, string? Email);

// The size sets how long the visit takes and how much material it uses
public sealed record BookingVehicle(
    Guid Id, string LicensePlate, string? Brand, string? Model, VehicleSize Size = VehicleSize.Medium);
