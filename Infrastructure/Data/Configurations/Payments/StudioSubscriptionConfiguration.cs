using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Payments;

public class StudioSubscriptionConfiguration : IEntityTypeConfiguration<StudioSubscription>
{
    public void Configure(EntityTypeBuilder<StudioSubscription> builder)
    {
        builder.ToTable("studio_subscription");
        builder.HasKey(x => x.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.PlanId).HasColumnName("plan_id");
        builder.Property(e => e.StripeSubscriptionId).HasColumnName("stripe_subscription_id");
        builder.Property(e => e.StripeStatus).HasColumnName("stripe_status");
        builder.Property(e => e.CurrentPeriodStart).HasColumnName("current_period_start");
        builder.Property(e => e.CurrentPeriodEnd).HasColumnName("current_period_end");
        builder.Property(e => e.CancelAtPeriodEnd).HasColumnName("cancel_at_period_end");
        builder.Property(e => e.CanceledAt).HasColumnName("canceled_at");
        builder.Property(e => e.EndedAt).HasColumnName("ended_at");
        builder.Property(e => e.TrialStart).HasColumnName("trial_start");
        builder.Property(e => e.TrialEnd).HasColumnName("trial_end");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Studio)
            .WithMany(x => x.StudioSubscriptions)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Plan)
            .WithMany(x => x.StudioSubscriptions)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}