using Customers.Contracts;
using Marketplace.Contracts;
using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Planning;
using Scheduling.Domain.Planning;
using Scheduling.Contracts;
using Scheduling.Domain.Requests;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;
using Shared.Kernel.Vehicles;

namespace Scheduling.Application.Requests.Submit;

public sealed class SubmitBookingRequestHandler(
    ISchedulingDbContext db, ITenantContext tenant, IPublicStudios publicStudios, ICustomerDirectory customers,
    BookingPlanner planner, TimeProvider clock)
{
    /// <summary>
    /// Sends the signed-in customer's request to a published studio. The prices are this studio's for the car's size,
    /// worked out here (never taken from the client). The studio is told through BookingRequestSubmitted.
    /// </summary>
    public async Task<Result<BookingRequestDto>> HandleAsync(
        SubmitBookingRequest request, string customerEmail, CancellationToken cancellationToken)
    {
        var userId = tenant.UserId ?? throw new InvalidOperationException("A request needs a signed-in customer.");

        var studio = await publicStudios.FindPublishedAsync(request.StudioId, cancellationToken);
        if (studio == null) return CustomerRequestErrors.StudioNotAvailable;

        var profile = await customers.FindProfileAsync(userId, cancellationToken);
        if (profile == null) return CustomerRequestErrors.ProfileRequired;

        RequestVehicle vehicle;
        if (request.VehicleId is { } vehicleId)
        {
            var saved = await customers.FindVehicleAsync(userId, vehicleId, cancellationToken);
            if (saved == null) return CustomerRequestErrors.VehicleNotFound;
            vehicle = new RequestVehicle(saved.LicensePlate, saved.Brand, saved.Model, VehicleSizes.Parse(saved.Size));
        }
        else
        {
            var typed = request.Vehicle!;
            vehicle = new RequestVehicle(typed.LicensePlate, typed.Brand, typed.Model, typed.Size!.Value);
        }

        var pending = await db.BookingRequestsOfAnyStudio.CountAsync(
            r => r.StudioId == studio.StudioId && r.CustomerUserId == userId && r.Status == OnlineRequestStatus.Pending,
            cancellationToken);
        if (pending >= SubmitLimits.MaxPendingPerStudio) return CustomerRequestErrors.TooManyPending;

        var services = await db.PriceAsync(studio.StudioId, request.ServiceIds, vehicle.Size, cancellationToken);
        if (services == null) return CustomerRequestErrors.UnknownServices;

        var timing = await CheckTimingAsync(studio.StudioId, request.PreferredAt, request.ServiceIds, vehicle, cancellationToken);
        if (!timing.Success) return timing.Error!;

        var bookingRequest = OnlineBookingRequest.Submit(
            studio.StudioId, studio.Name, new RequestCustomer(userId, profile.Name, profile.Phone, customerEmail),
            vehicle, services, request.PreferredAt, request.Notes);

        db.BookingRequests.Add(bookingRequest);
        db.Enqueue(new BookingRequestSubmitted(
            bookingRequest.StudioId, bookingRequest.Id, bookingRequest.StudioName, bookingRequest.CustomerName,
            bookingRequest.CustomerPhone, bookingRequest.VehicleLicensePlate, bookingRequest.PreferredAt,
            services.Select(s => s.Name).ToList()));
        await db.SaveChangesAsync(cancellationToken);

        return BookingRequestDto.From(bookingRequest);
    }

    // A studio that plans only takes requests the whole visit fits in its opening hours and no sooner than its lead
    // time. Whether a bay is free isn't checked: the studio decides, and other requests don't hold bays.
    private async Task<Result> CheckTimingAsync(
        Guid studioId, DateTime preferredAt, IReadOnlyCollection<Guid> serviceIds, RequestVehicle vehicle,
        CancellationToken cancellationToken)
    {
        var plan = await planner.LoadAsync(studioId, cancellationToken);
        if (!plan.Enabled) return Result.Ok();

        if (preferredAt < clock.GetUtcNow().UtcDateTime.AddHours(plan.Schedule.MinLeadHours)) return CustomerRequestErrors.TooSoon;

        var duration = await planner.DurationAsync(plan, serviceIds, vehicle.Size, cancellationToken);
        var visit = new TimeRange(preferredAt, preferredAt + duration);
        var open = SlotPlanner.OpeningRanges(
            plan.Schedule, plan.Zone, BookingPlanner.LocalDay(plan, visit.Start), BookingPlanner.LocalDay(plan, visit.End));

        return open.Any(range => range.Contains(visit)) ? Result.Ok() : CustomerRequestErrors.OutsideOpeningHours;
    }
}
