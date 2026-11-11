using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;

namespace Scheduling.Application.Planning.Bays.GetBays;

public sealed class GetBaysHandler(ISchedulingDbContext db)
{
    /// <summary>
    /// The studio's bays (tenant-filtered), active first. A studio has a handful, so the list isn't paged.
    /// </summary>
    public async Task<List<BayDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var bays = await db.Bays.AsNoTracking()
            .OrderByDescending(b => b.IsActive).ThenBy(b => b.Name).ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

        return bays.Select(BayDto.From).ToList();
    }
}
