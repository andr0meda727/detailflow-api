using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;

namespace Scheduling.Application.Catalog.GetServices;

public sealed class GetServicesHandler(ISchedulingDbContext db)
{
    // A studio has a handful of services, so the list is not paged
    public async Task<List<ServiceResponseDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var services = await db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        return services.Select(ServiceResponseDto.From).ToList();
    }
}
