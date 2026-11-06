using Shared.Kernel.Auditing;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Scheduling.Domain.Bookings;

/// <summary>
/// A visit at a studio. It is open while Created; completing or cancelling it finishes it for good.
/// </summary>
public class Booking : IAuditableEntity, ITenantEntity
{
    private readonly List<BookingServiceLine> _services = [];

    // For EF Core
    private Booking() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    // Client and vehicle are owned by CRM. The booking keeps their IDs plus a copy of what it displays,
    // refreshed from CRM events, so it never has to read CRM's tables.
    public Guid ClientId { get; private set; }

    public string ClientName { get; private set; } = null!;

    public string ClientPhone { get; private set; } = null!;

    public string? ClientEmail { get; private set; }

    public Guid VehicleId { get; private set; }

    public string VehicleLicensePlate { get; private set; } = null!;

    public string? VehicleBrand { get; private set; }

    public string? VehicleModel { get; private set; }

    public DateTime ScheduledAt { get; private set; }

    // Set when the booking is finished, by completion or by cancellation
    public DateTime? CompletedAt { get; private set; }

    public Guid StatusId { get; private set; }

    // In cents
    public long? TotalPrice { get; private set; }

    public string? Notes { get; private set; }

    public Guid? CreatedBy { get; private set; }

    // The customer account the booking was requested from on the public map; null for bookings staff entered
    public Guid? CustomerUserId { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public BookingStatus Status { get; private set; } = null!;

    public IReadOnlyCollection<BookingServiceLine> Services => _services;

    public bool IsFinished => StatusId != BookingStatus.Created;

    public static Booking Create(
        BookingClient client, BookingVehicle vehicle, DateTime scheduledAt, long priceInCents, string? notes,
        IReadOnlyCollection<Guid> serviceIds, Guid? createdBy, Guid? customerUserId = null)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(), StatusId = BookingStatus.Created, CreatedBy = createdBy, CustomerUserId = customerUserId
        };
        booking.Apply(client, vehicle, scheduledAt, priceInCents, notes, serviceIds);
        return booking;
    }

    public Result Edit(
        BookingClient client, BookingVehicle vehicle, DateTime scheduledAt, long priceInCents, string? notes,
        IReadOnlyCollection<Guid> serviceIds)
    {
        if (IsFinished) return BookingErrors.AlreadyFinished;

        Apply(client, vehicle, scheduledAt, priceInCents, notes, serviceIds);
        return Result.Ok();
    }

    public Result Complete(DateTime now) => Finish(BookingStatus.Completed, now);

    public Result Cancel(DateTime now) => Finish(BookingStatus.Cancelled, now);

    /// <summary>
    /// Moves to the requested status through the normal transitions; staying Created is a no-op.
    /// </summary>
    public Result ChangeStatus(Guid statusId, DateTime now)
    {
        if (statusId == StatusId) return Result.Ok();
        if (statusId == BookingStatus.Completed) return Complete(now);
        if (statusId == BookingStatus.Cancelled) return Cancel(now);

        // The only status left is Created, and a finished booking can't be reopened
        return BookingErrors.AlreadyFinished;
    }

    private Result Finish(Guid statusId, DateTime now)
    {
        if (IsFinished) return BookingErrors.AlreadyFinished;

        StatusId = statusId;
        CompletedAt = now;
        return Result.Ok();
    }

    private void Apply(
        BookingClient client, BookingVehicle vehicle, DateTime scheduledAt, long priceInCents, string? notes,
        IReadOnlyCollection<Guid> serviceIds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(priceInCents);
        if (serviceIds.Count == 0) throw new ArgumentException("A booking needs at least one service.", nameof(serviceIds));

        ClientId = client.Id;
        ClientName = client.Name;
        ClientPhone = client.Phone;
        ClientEmail = client.Email;

        VehicleId = vehicle.Id;
        VehicleLicensePlate = vehicle.LicensePlate;
        VehicleBrand = vehicle.Brand;
        VehicleModel = vehicle.Model;

        ScheduledAt = scheduledAt;
        TotalPrice = priceInCents;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        // Change only what differs, so unchanged lines keep their rows
        var wanted = serviceIds.ToHashSet();
        _services.RemoveAll(line => !wanted.Contains(line.ServiceId));
        foreach (var serviceId in wanted.Where(id => _services.All(line => line.ServiceId != id)))
        {
            _services.Add(new BookingServiceLine(Id, serviceId));
        }
    }
}
