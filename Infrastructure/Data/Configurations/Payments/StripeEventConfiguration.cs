using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Payments;

public class StripeEventConfiguration : IEntityTypeConfiguration<StripeEvent>
{
    public void Configure(EntityTypeBuilder<StripeEvent> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("stripe_event_pkey");

        builder
            .ToTable("stripe_event", "d");

        builder
            .HasIndex(e => e.StripeEventId, "stripe_event_stripe_event_id_key")
            .IsUnique();

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(e => e.EventType)
            .HasColumnName("event_type");

        builder.Property(e => e.Payload)
            .HasColumnType("jsonb")
            .HasColumnName("payload");

        builder.Property(e => e.Processed)
            .HasColumnName("processed");

        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(e => e.StripeEventId)
            .HasColumnName("stripe_event_id");

        builder.Property(e => e.StudioId)
            .HasColumnName("studio_id");

        builder.Property(e => e.SubscriptionId)
            .HasColumnName("subscription_id");

        builder
            .HasOne(d => d.Studio)
            .WithMany(p => p.StripeEvents)
            .HasForeignKey(d => d.StudioId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("stripe_event_studio_id_fkey");

        builder
            .HasOne(d => d.Subscription)
            .WithMany(p => p.StripeEvents)
            .HasForeignKey(d => d.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("stripe_event_subscription_id_fkey");
    }
}