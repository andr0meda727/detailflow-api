using Crm.Contracts;
using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Planning;
using Scheduling.Contracts;
using Scheduling.Domain.Bookings;
using Scheduling.Domain.Requests;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Scheduling.Application.Requests.Accept;

public sealed class AcceptBookingRequestHandler(
    ISchedulingDbContext db, IClientDirectory clients, ITenantContext tenant, TimeProvider clock, BookingPlanner planner)
{
    /// <summary>
    /// Turns a pending request into a normal booking: CRM finds or creates the studio's client and car for the
    /// customer, the booking is created at the agreed time and price, and the customer is told. A studio that plans puts
    /// the car on the chosen bay, or the best-fitting free one; when none is free the request stays pending.
    /// </summary>
    public async Task<Result<BookingRequestDto>> HandleAsync(
        Guid id, AcceptBookingRequest decision, CancellationToken cancellationToken)
    {
        var request = await db.BookingRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request == null) return RequestErrors.NotFound;
        if (!request.IsPending) return RequestErrors.AlreadyDecided;

        var scheduledAt = decision.ScheduledAt ?? request.PreferredAt;
        var serviceIds = request.Services.Select(s => s.ServiceId).ToList();

        // Before CRM is touched, so a time that doesn't fit changes nothing
        var placement = await planner.PlaceAsync(
            request.StudioId, scheduledAt, serviceIds, request.VehicleSize, decision.BayId,
            exceptBookingId: null, assignBay: true, cancellationToken);
        if (!placement.Success) return placement.Error!;

        // CRM saves on its own; it is safe to repeat, so a failure after this point leaves nothing half done
        var parties = await clients.EnsureOnlineCustomerAsync(new OnlineCustomer(
            request.CustomerUserId, request.CustomerName, request.CustomerPhone, request.CustomerEmail,
            request.VehicleLicensePlate, request.VehicleBrand, request.VehicleModel, request.VehicleSize.ToString()),
            cancellationToken);

        var priceInCents = decision.Price is { } price ? PriceRules.ToCents(price) : request.EstimatedPriceInCents;

        var booking = Booking.Create(
            new BookingClient(parties.Client.Id, parties.Client.Name, parties.Client.Phone, parties.Client.Email),
            // The size the customer gave is the one the price was worked out for
            new BookingVehicle(
                parties.Vehicle.Id, parties.Vehicle.LicensePlate, parties.Vehicle.Brand, parties.Vehicle.Model,
                request.VehicleSize),
            scheduledAt, priceInCents, request.Notes, serviceIds,
            createdBy: tenant.UserId, customerUserId: request.CustomerUserId);
        booking.Place(placement.Value!.EndsAt, placement.Value.BayId);
        db.Bookings.Add(booking);

        var accepted = request.Accept(booking.Id, clock.GetUtcNow().UtcDateTime);
        if (!accepted.Success) return accepted.Error!;

        db.Enqueue(new BookingRequestDecided(
            request.StudioId, request.Id, request.StudioName, request.CustomerName, request.CustomerEmail,
            Accepted: true, scheduledAt, RejectionReason: null));
        var saved = await db.SaveChangesCheckedAsync(cancellationToken);
        if (!saved.Success) return saved.Error!;

        return BookingRequestDto.From(request,
            new RequestBookingView(booking.Id, booking.ScheduledAt, "Created", booking.TotalPrice / 100m));
    }
}
