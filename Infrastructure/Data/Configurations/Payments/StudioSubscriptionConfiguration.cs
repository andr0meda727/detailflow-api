using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Payments;

public class StudioSubscriptionConfiguration : IEntityTypeConfiguration<StudioSubscription>
{
    public void Configure(EntityTypeBuilder<StudioSubscription> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("studio_subscription_pkey");

        builder
            .ToTable("studio_subscription", "d");

        builder
            .HasIndex(e => e.StripeSubscriptionId, "studio_subscription_stripe_subscription_id_key")
            .IsUnique();

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.CancelAtPeriodEnd)
            .HasColumnName("cancel_at_period_end");

        builder.Property(e => e.CanceledAt)
            .HasColumnName("canceled_at");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.CurrentPeriodEnd)
            .HasColumnName("current_period_end");

        builder.Property(e => e.CurrentPeriodStart)
            .HasColumnName("current_period_start");

        builder.Property(e => e.EndedAt)
            .HasColumnName("ended_at");

        builder.Property(e => e.PlanId)
            .HasColumnName("plan_id");

        builder.Property(e => e.StripeStatus)
            .HasColumnName("stripe_status");

        builder.Property(e => e.StripeSubscriptionId)
            .HasColumnName("stripe_subscription_id");

        builder.Property(e => e.StudioId)
            .HasColumnName("studio_id");

        builder.Property(e => e.TrialEnd)
            .HasColumnName("trial_end");

        builder.Property(e => e.TrialStart)
            .HasColumnName("trial_start");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder
            .HasOne(d => d.Plan)
            .WithMany(p => p.StudioSubscriptions)
            .HasForeignKey(d => d.PlanId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("studio_subscription_plan_id_fkey");

        builder
            .HasOne(d => d.Studio)
            .WithMany(p => p.StudioSubscriptions)
            .HasForeignKey(d => d.StudioId)
            .HasConstraintName("studio_subscription_studio_id_fkey");
    }
}