using Marketplace.Contracts;
using Scheduling.Application.Abstractions;
using Shared.Kernel.Results;

namespace Scheduling.Application.Requests.Estimate;

public sealed class EstimateHandler(ISchedulingDbContext db, IPublicStudios publicStudios)
{
    /// <summary>
    /// The price of the chosen services for a car of this size at a published studio, before sending a request.
    /// Computed the same way as the request's own estimate.
    /// </summary>
    public async Task<Result<EstimateDto>> HandleAsync(Guid studioId, EstimateRequest request, CancellationToken cancellationToken)
    {
        if (await publicStudios.FindPublishedAsync(studioId, cancellationToken) == null)
        {
            return CustomerRequestErrors.StudioNotAvailable;
        }

        var services = await db.PriceAsync(studioId, request.ServiceIds, request.VehicleSize!.Value, cancellationToken);
        if (services == null) return CustomerRequestErrors.UnknownServices;

        return EstimateDto.From(services);
    }
}
