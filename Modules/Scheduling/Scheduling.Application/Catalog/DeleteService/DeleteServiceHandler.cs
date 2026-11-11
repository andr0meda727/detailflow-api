using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Shared.Kernel.Results;

namespace Scheduling.Application.Catalog.DeleteService;

public sealed class DeleteServiceHandler(ISchedulingDbContext db, TimeProvider clock)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id && s.IsActive, cancellationToken);
        if (service == null) return SchedulingErrors.ServiceNotFound(id);

        // Deactivated rather than deleted: past bookings still show it
        service.Deactivate();
        await db.EnqueueCatalogChangedAsync(service.StudioId, clock.GetUtcNow().UtcDateTime, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
