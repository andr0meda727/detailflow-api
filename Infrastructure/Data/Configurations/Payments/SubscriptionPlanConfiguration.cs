using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Payments;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("subscription_plan_pkey");

        builder
            .ToTable("subscription_plan", "d");

        builder
            .HasIndex(e => e.StripePriceId, "subscription_plan_stripe_price_id_key")
            .IsUnique();

        builder
            .HasIndex(e => e.StripeProductId, "subscription_plan_stripe_product_id_key")
            .IsUnique();

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.Amount)
            .HasColumnName("amount");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.Currency)
            .HasDefaultValueSql("'pln'::text")
            .HasColumnName("currency");

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name");

        builder.Property(e => e.Features)
            .HasColumnType("jsonb")
            .HasColumnName("features");

        builder.Property(e => e.Interval)
            .HasColumnName("interval");

        builder.Property(e => e.IntervalCount)
            .HasDefaultValue(1)
            .HasColumnName("interval_count");

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(e => e.MaxBookingsPerMonth)
            .HasColumnName("max_bookings_per_month");

        builder.Property(e => e.MaxUsers)
            .HasColumnName("max_users");

        builder.Property(e => e.MaxVehicles)
            .HasColumnName("max_vehicles");

        builder.Property(e => e.Name)
            .HasColumnName("name");

        builder.Property(e => e.StripePriceId)
            .HasColumnName("stripe_price_id");

        builder.Property(e => e.StripeProductId)
            .HasColumnName("stripe_product_id");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");
    }
}