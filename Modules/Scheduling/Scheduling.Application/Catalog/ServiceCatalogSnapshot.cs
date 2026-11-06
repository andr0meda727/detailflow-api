using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Contracts;
using Scheduling.Domain.Catalog;

namespace Scheduling.Application.Catalog;

public static class ServiceCatalogSnapshot
{
    public static ServiceCatalogChanged From(Guid studioId, IEnumerable<Service> services, DateTime changedAt) => new(
        studioId, changedAt,
        services.Where(s => s.IsActive)
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .Select(s => new CatalogService(s.Id, s.Name, s.Category.ToString(),
                s.Prices.OrderBy(p => p.Size).Select(p => new CatalogPrice(p.Size.ToString(), p.AmountInCents)).ToList()))
            .ToList());

    /// <summary>
    /// Queues the studio's catalogue as it will be after the pending changes, so it is saved by the same
    /// SaveChangesAsync as the change itself. Call it after adding or changing the service.
    /// </summary>
    public static async Task EnqueueCatalogChangedAsync(
        this ISchedulingDbContext db, Guid studioId, DateTime changedAt, CancellationToken cancellationToken)
    {
        // The query is tenant-filtered and returns the tracked, already changed instances. A service added in this
        // unit of work is only in Local (it gets its studio on save), so both are combined.
        var stored = await db.Services.Where(s => s.IsActive).ToListAsync(cancellationToken);

        db.Enqueue(From(studioId, stored.Union(db.Services.Local), changedAt));
    }
}
