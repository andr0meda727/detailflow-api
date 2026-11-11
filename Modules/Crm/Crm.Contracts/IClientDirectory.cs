namespace Crm.Contracts;

/// <summary>
/// The CRM module's public read API for other modules. Lookups are scoped to the current studio.
/// Callers get summaries, never CRM entities, so this can later become a remote call.
/// </summary>
public interface IClientDirectory
{
    /// <summary>
    /// Returns the client and vehicle of a booking, or null when either is missing
    /// or the vehicle doesn't belong to the client.
    /// </summary>
    Task<BookingParties?> FindBookingPartiesAsync(Guid clientId, Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The current studio's client and car for a customer who booked online, created when missing: the client is the
    /// one linked to the account, else the one with the same phone (which then gets linked), else a new one; the car
    /// is the client's car with the same plate, else a new one. Safe to call again for the same customer.
    /// </summary>
    Task<BookingParties> EnsureOnlineCustomerAsync(OnlineCustomer customer, CancellationToken cancellationToken = default);
}

public sealed record ClientSummary(Guid Id, string Name, string Phone, string? Email);

// Size is the size class by name, as in VehicleDetailsChanged
public sealed record VehicleSummary(
    Guid Id, Guid ClientId, string LicensePlate, string? Brand, string? Model, string Size = "Medium");

public sealed record BookingParties(ClientSummary Client, VehicleSummary Vehicle);

/// <summary>
/// A customer and their car as they asked for a visit online. <paramref name="Size"/> is the size class by name.
/// </summary>
public sealed record OnlineCustomer(
    Guid UserId, string Name, string Phone, string Email, string LicensePlate, string? Brand, string? Model, string Size);
