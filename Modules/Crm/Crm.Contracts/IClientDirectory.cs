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
}

public sealed record ClientSummary(Guid Id, string Name, string Phone, string? Email);

// Size is the size class by name, as in VehicleDetailsChanged
public sealed record VehicleSummary(
    Guid Id, Guid ClientId, string LicensePlate, string? Brand, string? Model, string Size = "Medium");

public sealed record BookingParties(ClientSummary Client, VehicleSummary Vehicle);
