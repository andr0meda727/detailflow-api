using Crm.Contracts;
using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Contracts;
using Scheduling.Domain.Bookings;
using Scheduling.Domain.Requests;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Scheduling.Application.Requests.Accept;

public sealed class AcceptBookingRequestHandler(
    ISchedulingDbContext db, IClientDirectory clients, ITenantContext tenant, TimeProvider clock)
{
    /// <summary>
    /// Turns a pending request into a normal booking: CRM finds or creates the studio's client and car for the
    /// customer, the booking is created at the agreed time and price, and the customer is told.
    /// </summary>
    public async Task<Result<BookingRequestDto>> HandleAsync(
        Guid id, AcceptBookingRequest decision, CancellationToken cancellationToken)
    {
        var request = await db.BookingRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request == null) return RequestErrors.NotFound;
        if (!request.IsPending) return RequestErrors.AlreadyDecided;

        // CRM saves on its own; it is safe to repeat, so a failure after this point leaves nothing half done
        var parties = await clients.EnsureOnlineCustomerAsync(new OnlineCustomer(
            request.CustomerUserId, request.CustomerName, request.CustomerPhone, request.CustomerEmail,
            request.VehicleLicensePlate, request.VehicleBrand, request.VehicleModel, request.VehicleSize.ToString()),
            cancellationToken);

        var scheduledAt = decision.ScheduledAt ?? request.PreferredAt;
        var priceInCents = decision.Price is { } price ? PriceRules.ToCents(price) : request.EstimatedPriceInCents;

        var booking = Booking.Create(
            new BookingClient(parties.Client.Id, parties.Client.Name, parties.Client.Phone, parties.Client.Email),
            // The size the customer gave is the one the price was worked out for
            new BookingVehicle(
                parties.Vehicle.Id, parties.Vehicle.LicensePlate, parties.Vehicle.Brand, parties.Vehicle.Model,
                request.VehicleSize),
            scheduledAt, priceInCents, request.Notes, request.Services.Select(s => s.ServiceId).ToList(),
            createdBy: tenant.UserId, customerUserId: request.CustomerUserId);
        db.Bookings.Add(booking);

        var accepted = request.Accept(booking.Id, clock.GetUtcNow().UtcDateTime);
        if (!accepted.Success) return accepted.Error!;

        db.Enqueue(new BookingRequestDecided(
            request.StudioId, request.Id, request.StudioName, request.CustomerName, request.CustomerEmail,
            Accepted: true, scheduledAt, RejectionReason: null));
        await db.SaveChangesAsync(cancellationToken);

        return BookingRequestDto.From(request,
            new RequestBookingView(booking.Id, booking.ScheduledAt, "Created", booking.TotalPrice / 100m));
    }
}
