using Shared.Kernel.Auditing;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;
using Shared.Kernel.Vehicles;

namespace Scheduling.Domain.Requests;

public enum OnlineRequestStatus
{
    Pending,
    Accepted,
    Rejected,
    Withdrawn
}

public sealed record RequestCustomer(Guid UserId, string Name, string Phone, string Email);

public sealed record RequestVehicle(string LicensePlate, string? Brand, string? Model, VehicleSize Size);

/// <summary>
/// A service the customer asked for, with its price for their car at the time (null: quoted on request).
/// </summary>
public sealed record RequestedService(Guid ServiceId, string Name, long? PriceInCents);

/// <summary>
/// A customer asked a studio for a visit from the public map. It keeps a copy of who asked, for which car, what and
/// for when, so it reads the same later whatever changes in the customer's account or the studio's prices. Staff
/// accept it (which creates a normal booking) or reject it; the customer can withdraw it while it is pending.
/// It belongs to the studio (tenant data) but is created by the customer, outside any tenant.
/// </summary>
public class OnlineBookingRequest : IAuditableEntity, ITenantEntity
{
    public const int MaxNotesLength = 1000;

    private readonly List<RequestedService> _services = [];

    // For EF Core
    private OnlineBookingRequest() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    // The studio's public name when the request was sent, for the customer's history
    public string StudioName { get; private set; } = null!;

    public Guid CustomerUserId { get; private set; }

    public string CustomerName { get; private set; } = null!;

    public string CustomerPhone { get; private set; } = null!;

    public string CustomerEmail { get; private set; } = null!;

    public string VehicleLicensePlate { get; private set; } = null!;

    public string? VehicleBrand { get; private set; }

    public string? VehicleModel { get; private set; }

    public VehicleSize VehicleSize { get; private set; }

    public DateTime PreferredAt { get; private set; }

    public string? Notes { get; private set; }

    // Sum of the services that have a price for this car, in cents
    public long EstimatedPriceInCents { get; private set; }

    // Some service is quoted on request, so the estimate is a lower bound
    public bool HasUnpricedServices { get; private set; }

    public OnlineRequestStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    // The booking created when the studio accepted
    public Guid? BookingId { get; private set; }

    public DateTime? DecidedAt { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyCollection<RequestedService> Services => _services;

    public bool IsPending => Status == OnlineRequestStatus.Pending;

    public static OnlineBookingRequest Submit(
        Guid studioId, string studioName, RequestCustomer customer, RequestVehicle vehicle,
        IReadOnlyCollection<RequestedService> services, DateTime preferredAt, string? notes)
    {
        if (studioId == Guid.Empty) throw new ArgumentException("A request goes to a studio.", nameof(studioId));
        if (customer.UserId == Guid.Empty) throw new ArgumentException("A request comes from an account.", nameof(customer));
        if (services.Count == 0) throw new ArgumentException("A request needs at least one service.", nameof(services));
        if (services.DistinctBy(s => s.ServiceId).Count() != services.Count)
            throw new ArgumentException("Each service can be asked for once.", nameof(services));
        if (!Enum.IsDefined(vehicle.Size)) throw new ArgumentOutOfRangeException(nameof(vehicle), vehicle.Size, "Unknown vehicle size.");
        ArgumentException.ThrowIfNullOrWhiteSpace(vehicle.LicensePlate);

        var request = new OnlineBookingRequest
        {
            Id = Guid.NewGuid(),
            StudioId = studioId,
            StudioName = studioName.Trim(),
            CustomerUserId = customer.UserId,
            CustomerName = customer.Name.Trim(),
            CustomerPhone = customer.Phone.Trim(),
            CustomerEmail = customer.Email.Trim(),
            VehicleLicensePlate = vehicle.LicensePlate.Trim().ToUpperInvariant(),
            VehicleBrand = string.IsNullOrWhiteSpace(vehicle.Brand) ? null : vehicle.Brand.Trim(),
            VehicleModel = string.IsNullOrWhiteSpace(vehicle.Model) ? null : vehicle.Model.Trim(),
            VehicleSize = vehicle.Size,
            PreferredAt = preferredAt,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            EstimatedPriceInCents = services.Sum(s => s.PriceInCents ?? 0),
            HasUnpricedServices = services.Any(s => s.PriceInCents == null),
            Status = OnlineRequestStatus.Pending
        };
        request._services.AddRange(services);
        return request;
    }

    public Result Accept(Guid bookingId, DateTime now)
    {
        if (!IsPending) return RequestErrors.AlreadyDecided;

        Status = OnlineRequestStatus.Accepted;
        BookingId = bookingId;
        DecidedAt = now;
        return Result.Ok();
    }

    public Result Reject(string? reason, DateTime now)
    {
        if (!IsPending) return RequestErrors.AlreadyDecided;

        Status = OnlineRequestStatus.Rejected;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        DecidedAt = now;
        return Result.Ok();
    }

    /// <summary>
    /// The customer changed their mind before the studio answered.
    /// </summary>
    public Result Withdraw(DateTime now)
    {
        if (!IsPending) return RequestErrors.AlreadyDecided;

        Status = OnlineRequestStatus.Withdrawn;
        DecidedAt = now;
        return Result.Ok();
    }
}

public static class RequestErrors
{
    public static readonly Error NotFound =
        Error.NotFound("BOOKING_REQUEST_NOT_FOUND", "Nie znaleziono prośby o rezerwację.");

    public static readonly Error AlreadyDecided = Error.Conflict(
        "BOOKING_REQUEST_ALREADY_DECIDED", "Ta prośba o rezerwację została już rozpatrzona lub wycofana.");
}
