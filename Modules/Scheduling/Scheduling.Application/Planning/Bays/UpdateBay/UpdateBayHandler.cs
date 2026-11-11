using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Shared.Kernel.Results;

namespace Scheduling.Application.Planning.Bays.UpdateBay;

public sealed class UpdateBayHandler(ISchedulingDbContext db)
{
    public async Task<Result<BayDto>> HandleAsync(Guid id, BayRequest request, CancellationToken cancellationToken)
    {
        // Tenant-filtered, so another studio's bay is simply not found
        var bay = await db.Bays.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (bay == null) return BayErrors.NotFound;

        bay.Rename(request.Name);
        await db.SaveChangesAsync(cancellationToken);

        return BayDto.From(bay);
    }
}
