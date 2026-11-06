using Scheduling.Application.Abstractions;
using Scheduling.Contracts;
using Scheduling.Domain.Catalog;
using Shared.Kernel.Tenancy;

namespace Scheduling.Application.Catalog.CreateService;

public sealed class CreateServiceHandler(ISchedulingDbContext db, ITenantContext tenant, TimeProvider clock)
{
    public async Task<ServiceResponseDto> HandleAsync(ServiceRequest request, CancellationToken cancellationToken)
    {
        var studioId = tenant.StudioId ?? throw new InvalidOperationException("Creating a service needs a resolved studio.");
        var service = Service.Create(request.Name, request.Category ?? ServiceCategory.Other, request.ToPrices());

        db.Services.Add(service);
        await db.EnqueueCatalogChangedAsync(studioId, clock.GetUtcNow().UtcDateTime, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResponseDto.From(service);
    }
}
