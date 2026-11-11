using Inventory.Application.Consumption;
using Inventory.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scheduling.Contracts;
using Shared.Kernel.Vehicles;

namespace Inventory.Infrastructure.Consumers;

/// <summary>
/// Takes the materials a completed visit used off stock. Delivery is at least once, so a booking already recorded
/// is skipped; and if two deliveries race, the unique booking index lets only one save and the other is treated as
/// done.
/// </summary>
public class BookingConsumptionConsumer(ConsumptionRecorder recorder) : IConsumer<BookingCompleted>
{
    public async Task Consume(ConsumeContext<BookingCompleted> context)
    {
        var message = context.Message;

        try
        {
            await recorder.RecordAsync(
                message.StudioId, message.BookingId, VehicleSizes.Parse(message.VehicleSize), message.ServiceIds,
                message.CompletedAt, context.CancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
                                          {
                                              SqlState: PostgresErrorCodes.UniqueViolation,
                                              ConstraintName: InventoryDbContext.BookingConsumptionKey
                                          })
        {
            // Another delivery of the same event recorded it first
        }
    }
}
