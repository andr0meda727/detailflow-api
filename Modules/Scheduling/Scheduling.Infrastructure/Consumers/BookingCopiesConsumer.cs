using Crm.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Scheduling.Infrastructure.Persistence;
using Shared.Infrastructure.Persistence;
using Shared.Kernel.Vehicles;

namespace Scheduling.Infrastructure.Consumers;

/// <summary>
/// Keeps the client and vehicle copies on bookings in step with CRM. Runs outside any request, so it
/// bypasses the tenant filter and scopes every update by the studio carried in the event instead.
/// </summary>
public class BookingCopiesConsumer(SchedulingDbContext db)
    : IConsumer<ClientDetailsChanged>, IConsumer<VehicleDetailsChanged>
{
    public async Task Consume(ConsumeContext<ClientDetailsChanged> consumeContext)
    {
        var message = consumeContext.Message;

        await db.Bookings
            .IgnoreQueryFilters([TenantDbContext.TenantFilter])
            .Where(b => b.StudioId == message.StudioId && b.ClientId == message.ClientId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(b => b.ClientName, message.Name)
                .SetProperty(b => b.ClientPhone, message.Phone)
                .SetProperty(b => b.ClientEmail, message.Email)
                .SetProperty(b => b.UpdatedAt, DateTime.UtcNow), consumeContext.CancellationToken);
    }

    public async Task Consume(ConsumeContext<VehicleDetailsChanged> consumeContext)
    {
        var message = consumeContext.Message;
        var size = VehicleSizes.Parse(message.Size);

        // A vehicle handed over to another client keeps its past bookings with the old client
        await db.Bookings
            .IgnoreQueryFilters([TenantDbContext.TenantFilter])
            .Where(b => b.StudioId == message.StudioId && b.VehicleId == message.VehicleId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(b => b.VehicleLicensePlate, message.LicensePlate)
                .SetProperty(b => b.VehicleBrand, message.Brand)
                .SetProperty(b => b.VehicleModel, message.Model)
                .SetProperty(b => b.VehicleSize, size)
                .SetProperty(b => b.UpdatedAt, DateTime.UtcNow), consumeContext.CancellationToken);
    }
}
