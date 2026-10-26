using Scheduling.Application.Abstractions;
using Scheduling.Domain.Catalog;

namespace Scheduling.Application.Catalog.CreateService;

public sealed class CreateServiceHandler(ISchedulingDbContext db)
{
    public async Task<ServiceResponseDto> HandleAsync(ServiceRequest request, CancellationToken cancellationToken)
    {
        var service = Service.Create(request.Name);

        db.Services.Add(service);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResponseDto.From(service);
    }
}
