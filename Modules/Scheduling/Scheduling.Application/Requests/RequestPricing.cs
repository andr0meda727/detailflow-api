using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Requests;
using Shared.Kernel.Vehicles;

namespace Scheduling.Application.Requests;

public static class RequestPricing
{
    /// <summary>
    /// The studio's active services with the given IDs, each with its price for a car of this size (null when the
    /// studio quotes it on request). Returns null when any ID isn't one of the studio's active services. The caller
    /// is a customer, so there is no tenant: the studio is matched explicitly.
    /// </summary>
    public static async Task<List<RequestedService>?> PriceAsync(
        this ISchedulingDbContext db, Guid studioId, IReadOnlyCollection<Guid> serviceIds, VehicleSize size,
        CancellationToken cancellationToken)
    {
        var ids = serviceIds.Distinct().ToList();
        var services = await db.ServicesOfAnyStudio
            .AsNoTracking()
            .Where(s => s.StudioId == studioId && s.IsActive && ids.Contains(s.Id))
            .ToListAsync(cancellationToken);

        if (services.Count != ids.Count) return null;

        return services
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .Select(s => new RequestedService(s.Id, s.Name, s.PriceFor(size)))
            .ToList();
    }
}
