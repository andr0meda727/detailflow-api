namespace Scheduling.Contracts;

// Events about customers' online booking requests. Times are UTC; consumers show them in the studio's time zone.

/// <summary>
/// A customer asked the studio for a visit; the studio should answer it.
/// </summary>
public sealed record BookingRequestSubmitted(
    Guid StudioId, Guid RequestId, string StudioName, string CustomerName, string CustomerPhone,
    string VehicleLicensePlate, DateTime PreferredAt, IReadOnlyList<string> Services);

/// <summary>
/// The studio accepted (<paramref name="ScheduledAt"/> is the booked time) or rejected the request; the customer
/// should hear about it.
/// </summary>
public sealed record BookingRequestDecided(
    Guid StudioId, Guid RequestId, string StudioName, string CustomerName, string CustomerEmail,
    bool Accepted, DateTime? ScheduledAt, string? RejectionReason);
