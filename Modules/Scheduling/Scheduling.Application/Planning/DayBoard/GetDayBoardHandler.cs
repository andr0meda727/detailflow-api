using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Bookings;
using Scheduling.Domain.Planning;
using Scheduling.Domain.Requests;
using Shared.Kernel.Tenancy;

namespace Scheduling.Application.Planning.DayBoard;

public sealed record BoardBookingDto(
    Guid Id, DateTime Start, DateTime End, string ClientName, string VehicleLicensePlate, string Status);

public sealed record BoardGapDto(DateTime Start, DateTime End, int Minutes);

/// <summary>
/// One bay's day: its bookings, the open time still free and how much of the open time is booked.
/// </summary>
public sealed record BoardBayDto(
    Guid BayId, string Name, List<BoardBookingDto> Bookings, List<BoardGapDto> FreeGaps, double UtilisationPercent);

public sealed record BoardRequestDto(Guid Id, DateTime PreferredAt, string CustomerName, string VehicleLicensePlate);

/// <summary>
/// The studio's day at a glance. <see cref="Unassigned"/> are bookings with no bay (entered before planning, or while
/// it was off); <see cref="PendingRequests"/> are customers' requests for that day still waiting for an answer.
/// </summary>
public sealed record DayBoardDto(
    DateOnly Date, bool PlanningEnabled, int OpenMinutes, double UtilisationPercent, List<BoardBayDto> Bays,
    List<BoardBookingDto> Unassigned, List<BoardRequestDto> PendingRequests);

public sealed class GetDayBoardHandler(ISchedulingDbContext db, BookingPlanner planner, ITenantContext tenant)
{
    public async Task<DayBoardDto> HandleAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var studioId = tenant.StudioId ?? throw new InvalidOperationException("The day board needs a resolved studio.");
        var plan = await planner.LoadAsync(studioId, cancellationToken);
        var day = BookingPlanner.DaysAround(plan, date, date);

        // Tenant-filtered; cancelled bookings hold nothing, so they aren't on the board
        var bookings = await db.Bookings.AsNoTracking()
            .Include(b => b.Status)
            .Where(b => b.StatusId != BookingStatus.Cancelled && b.ScheduledAt < day.End && b.EndsAt > day.Start)
            .OrderBy(b => b.ScheduledAt).ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

        var requests = await db.BookingRequests.AsNoTracking()
            .Where(r => r.Status == OnlineRequestStatus.Pending && r.PreferredAt >= day.Start && r.PreferredAt < day.End)
            .OrderBy(r => r.PreferredAt).ThenBy(r => r.Id)
            .Select(r => new BoardRequestDto(r.Id, r.PreferredAt, r.CustomerName, r.VehicleLicensePlate))
            .ToListAsync(cancellationToken);

        var open = SlotPlanner.OpeningRanges(plan.Schedule, plan.Zone, date, date);
        var openMinutes = open.Sum(r => r.Length.TotalMinutes);

        var bays = plan.Bays.Select(bay =>
        {
            var onBay = bookings.Where(b => b.BayId == bay.Id).ToList();
            var taken = onBay.Select(b => b.Range).ToList();
            var gaps = TimeRange.Subtract(open, taken);
            var bookedMinutes = openMinutes - gaps.Sum(g => g.Length.TotalMinutes);

            return new BoardBayDto(
                bay.Id, bay.Name, onBay.Select(ToDto).ToList(),
                gaps.Select(g => new BoardGapDto(g.Start, g.End, (int)g.Length.TotalMinutes)).ToList(),
                Percent(bookedMinutes, openMinutes));
        }).ToList();

        var totalBooked = bays.Sum(b => b.UtilisationPercent * openMinutes / 100);

        return new DayBoardDto(
            date, plan.Enabled, (int)openMinutes, Percent(totalBooked, openMinutes * bays.Count), bays,
            bookings.Where(b => b.BayId == null || plan.Bays.All(bay => bay.Id != b.BayId)).Select(ToDto).ToList(),
            requests);
    }

    private static BoardBookingDto ToDto(Booking b) =>
        new(b.Id, b.ScheduledAt, b.EndsAt, b.ClientName, b.VehicleLicensePlate, b.Status.Name);

    // Booked share of the open time; time booked outside opening hours doesn't count
    private static double Percent(double part, double whole) => whole <= 0 ? 0 : Math.Round(100 * part / whole, 1);
}
