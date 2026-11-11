using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Catalog;
using Scheduling.Contracts;
using Scheduling.Infrastructure.Persistence;
using Shared.Infrastructure.Persistence;

namespace Scheduling.Infrastructure.Catalog;

public class ServiceCatalogSource(SchedulingDbContext db, TimeProvider clock) : IServiceCatalogSource
{
    public async Task<ServiceCatalogChanged> GetCatalogAsync(Guid studioId, CancellationToken cancellationToken = default)
    {
        // Callers may run without a tenant (or as another module), so the studio comes from the argument
        var services = await db.Services
            .IgnoreQueryFilters([TenantDbContext.TenantFilter])
            .AsNoTracking()
            .Where(s => s.StudioId == studioId && s.IsActive)
            .ToListAsync(cancellationToken);

        return ServiceCatalogSnapshot.From(studioId, services, clock.GetUtcNow().UtcDateTime);
    }
}
