using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// An integration event waiting to be published. Each module keeps these in its own schema.
/// </summary>
public class OutboxMessage
{
    // For EF Core
    private OutboxMessage() { }

    public Guid Id { get; private set; }

    // Assembly-qualified type name, used to deserialize the event before publishing
    public string Type { get; private set; } = null!;

    public string Content { get; private set; } = null!;

    public DateTime OccurredAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage Create(object integrationEvent, DateTime occurredAt)
    {
        var type = integrationEvent.GetType();

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = $"{type.FullName}, {type.Assembly.GetName().Name}",
            Content = JsonSerializer.Serialize(integrationEvent, type),
            OccurredAt = occurredAt
        };
    }

    public object Deserialize() =>
        JsonSerializer.Deserialize(Content, System.Type.GetType(Type, throwOnError: true)!)!;

    public void MarkProcessed(DateTime at) => ProcessedAt = at;

    public void MarkFailed(Exception error)
    {
        Attempts++;
        LastError = error.Message;
    }
}

public static class OutboxModelBuilderExtensions
{
    /// <summary>
    /// Adds the outbox table to the context's default schema.
    /// </summary>
    public static ModelBuilder AddOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_message");
            builder.HasKey(e => e.Id).HasName("outbox_message_pkey");

            builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(e => e.Type).HasColumnName("type");
            builder.Property(e => e.Content).HasColumnName("content").HasColumnType("jsonb");
            builder.Property(e => e.OccurredAt).HasColumnName("occurred_at");
            builder.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            builder.Property(e => e.Attempts).HasColumnName("attempts");
            builder.Property(e => e.LastError).HasColumnName("last_error");

            // The dispatcher only ever looks at unprocessed messages, oldest first
            builder.HasIndex(e => e.OccurredAt, "outbox_message_pending_idx").HasFilter("processed_at IS NULL");
        });

        return modelBuilder;
    }
}
