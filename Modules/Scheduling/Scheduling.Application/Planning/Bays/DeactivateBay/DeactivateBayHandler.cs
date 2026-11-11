using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Shared.Kernel.Results;

namespace Scheduling.Application.Planning.Bays.DeactivateBay;

public sealed class DeactivateBayHandler(ISchedulingDbContext db)
{
    /// <summary>
    /// Takes the bay out of planning. It isn't deleted, so past bookings keep showing where the car stood.
    /// </summary>
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var bay = await db.Bays.FirstOrDefaultAsync(b => b.Id == id && b.IsActive, cancellationToken);
        if (bay == null) return BayErrors.NotFound;

        bay.Deactivate();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
