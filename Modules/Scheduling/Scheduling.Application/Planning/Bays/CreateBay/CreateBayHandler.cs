using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Planning;
using Shared.Kernel.Results;

namespace Scheduling.Application.Planning.Bays.CreateBay;

public sealed class CreateBayHandler(ISchedulingDbContext db)
{
    public async Task<Result<BayDto>> HandleAsync(BayRequest request, CancellationToken cancellationToken)
    {
        var active = await db.Bays.CountAsync(b => b.IsActive, cancellationToken);
        if (active >= Bay.MaxActivePerStudio) return BayErrors.LimitReached;

        var bay = Bay.Create(request.Name);
        db.Bays.Add(bay);
        await db.SaveChangesAsync(cancellationToken);

        return BayDto.From(bay);
    }
}
