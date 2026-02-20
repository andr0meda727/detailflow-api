using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Payments;

public class StripeEventConfiguration : IEntityTypeConfiguration<StripeEvent>
{
    public void Configure(EntityTypeBuilder<StripeEvent> builder)
    {
        builder.ToTable("stripe_event");
        builder.HasKey(x => x.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StripeEventId).HasColumnName("stripe_event_id");
        builder.Property(e => e.EventType).HasColumnName("event_type");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
        builder.Property(e => e.Payload).HasColumnName("payload");
        builder.Property(e => e.Processed).HasColumnName("processed");
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at");
        builder.Property(e => e.ErrorMessage).HasColumnName("error_message");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.Payload).HasColumnType("jsonb");

        builder.HasOne(x => x.Studio)
            .WithMany(x => x.StripeEvents)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.SetNull);

        // StripeEvent -> StudioSubscription
        builder.HasOne(x => x.Subscription)
            .WithMany(x => x.StripeEvents)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}