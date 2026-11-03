using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Moves saved outbox messages of one module context onto the message bus.
/// Rows are claimed with FOR UPDATE SKIP LOCKED, so several API instances can run it side by side.
/// </summary>
public sealed class OutboxDispatcher<TContext>(
    IServiceScopeFactory scopeFactory,
    IBus bus,
    TimeProvider clock,
    ILogger<OutboxDispatcher<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private const int BatchSize = 50;

    // A message that keeps failing stays in the table for inspection instead of blocking the queue
    public const int MaxAttempts = 10;

    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            int dispatched;
            try
            {
                dispatched = await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox dispatch for {Context} failed", typeof(TContext).Name);
                dispatched = 0;
            }

            // Keep going while there is a backlog, otherwise wait a moment
            if (dispatched < BatchSize)
            {
                await Task.Delay(IdleDelay, clock, stoppingToken);
            }
        }
    }

    public async Task<int> DispatchBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        var table = db.Model.FindEntityType(typeof(OutboxMessage))!;
        var tableName = $"\"{table.GetSchema() ?? "public"}\".\"{table.GetTableName()}\"";

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var messages = await db.Set<OutboxMessage>()
            .FromSqlRaw($"""
                SELECT * FROM {tableName}
                WHERE processed_at IS NULL AND attempts < {MaxAttempts}
                ORDER BY occurred_at
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var integrationEvent = message.Deserialize();
                await bus.Publish(integrationEvent, integrationEvent.GetType(), cancellationToken);
                message.MarkProcessed(clock.GetUtcNow().UtcDateTime);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Publishing outbox message {Id} ({Type}) failed", message.Id, message.Type);
                message.MarkFailed(ex);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return messages.Count;
    }
}
