namespace Crm.Contracts;

// Integration events published by the CRM module. They carry the full current state, so consumers
// don't need to call back into CRM, and they stay plain records so they can travel over a broker.

public sealed record ClientDetailsChanged(Guid StudioId, Guid ClientId, string Name, string Phone, string? Email);

/// <summary>
/// <paramref name="Size"/> is the size class by name ("Small", "Medium", "Large", "XL"). It is null in messages
/// queued before sizes existed, which consumers read as Medium.
/// </summary>
public sealed record VehicleDetailsChanged(
    Guid StudioId, Guid VehicleId, Guid ClientId, string LicensePlate, string? Brand, string? Model,
    string? Size = null);
