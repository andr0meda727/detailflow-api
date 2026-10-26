using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;

namespace Scheduling.Application.Catalog.GetServices;

public sealed class GetServicesHandler(ISchedulingDbContext db)
{
    // A studio has a handful of services, so the list is not paged
    public async Task<List<ServiceResponseDto>> HandleAsync(CancellationToken cancellationToken)
    {
        return await db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new ServiceResponseDto(s.Id, s.Name))
            .ToListAsync(cancellationToken);
    }
}
