using Marketplace.Domain.Reviews;
using Marketplace.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scheduling.Contracts;

namespace Marketplace.Infrastructure.Consumers;

/// <summary>
/// Lets the customer review a visit once Scheduling says it was completed. Bookings staff entered have no customer
/// account and are skipped. The booking ID is the key, so a repeated or racing delivery records nothing twice.
/// </summary>
public class ReviewableVisitConsumer(MarketplaceDbContext db) : IConsumer<BookingCompleted>
{
    public async Task Consume(ConsumeContext<BookingCompleted> context)
    {
        var message = context.Message;
        if (message.CustomerUserId is not { } customerUserId) return;

        if (await db.ReviewableVisits.AnyAsync(v => v.BookingId == message.BookingId, context.CancellationToken)) return;

        db.ReviewableVisits.Add(ReviewableVisit.Record(message.BookingId, message.StudioId, customerUserId, message.CompletedAt));
        try
        {
            await db.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
                                          {
                                              SqlState: PostgresErrorCodes.UniqueViolation,
                                              ConstraintName: MarketplaceDbContext.ReviewableVisitKey
                                          })
        {
            // Another delivery of the same event got there first
        }
    }
}
