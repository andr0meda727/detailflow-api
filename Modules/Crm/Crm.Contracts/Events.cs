namespace Crm.Contracts;

// Integration events published by the CRM module. They carry the full current state, so consumers
// don't need to call back into CRM, and they stay plain records so they can travel over a broker.

public sealed record ClientDetailsChanged(Guid StudioId, Guid ClientId, string Name, string Phone, string? Email);

public sealed record VehicleDetailsChanged(
    Guid StudioId, Guid VehicleId, Guid ClientId, string LicensePlate, string? Brand, string? Model);
