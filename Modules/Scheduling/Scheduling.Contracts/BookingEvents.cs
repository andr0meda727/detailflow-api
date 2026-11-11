namespace Scheduling.Contracts;

/// <summary>
/// A visit was completed. Inventory takes the materials it used off stock; Marketplace can let the customer
/// (<paramref name="CustomerUserId"/>, null for bookings staff entered) review it. <paramref name="VehicleSize"/> is the
/// size by name ("Small", "Medium", "Large", "XL"). A completed booking is final, so this comes once per booking
/// (though, delivered at least once, a consumer may see it again).
/// </summary>
public sealed record BookingCompleted(
    Guid StudioId, Guid BookingId, Guid? CustomerUserId, string VehicleSize, IReadOnlyList<Guid> ServiceIds,
    DateTime CompletedAt);

/// <summary>
/// Open bookings of a studio in a UTC range (end exclusive), for modules that plan ahead, e.g. Inventory forecasting
/// the materials booked visits will use. Works without a tenant: the studio is passed in.
/// </summary>
public interface IUpcomingBookingsSource
{
    Task<IReadOnlyList<UpcomingBooking>> GetUpcomingAsync(
        Guid studioId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default);
}

public sealed record UpcomingBooking(Guid BookingId, DateTime ScheduledAt, string VehicleSize, IReadOnlyList<Guid> ServiceIds);
