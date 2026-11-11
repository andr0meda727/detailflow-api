using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Shared.Kernel.Results;

namespace Scheduling.Application.Planning.Bays.DeactivateBay;

public sealed class DeactivateBayHandler(ISchedulingDbContext db, TimeProvider clock)
{
    /// <summary>
    /// Takes the bay out of planning. It isn't deleted, so past bookings keep showing where the car stood. A bay with
    /// open bookings still to come can't go: staff move them first.
    /// </summary>
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var bay = await db.Bays.FirstOrDefaultAsync(b => b.Id == id && b.IsActive, cancellationToken);
        if (bay == null) return BayErrors.NotFound;

        var now = clock.GetUtcNow().UtcDateTime;
        if (await db.Bookings.AnyAsync(
                b => b.BayId == id && b.StatusId == Domain.Bookings.BookingStatus.Created && b.EndsAt > now, cancellationToken))
        {
            return BayErrors.HasUpcomingBookings;
        }

        bay.Deactivate();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
