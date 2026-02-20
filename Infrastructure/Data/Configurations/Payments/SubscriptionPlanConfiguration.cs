using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Payments;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plan");
        builder.HasKey(x => x.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Name).HasColumnName("name");
        builder.Property(e => e.DisplayName).HasColumnName("display_name");
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.StripeProductId).HasColumnName("stripe_product_id");
        builder.Property(e => e.StripePriceId).HasColumnName("stripe_price_id");
        builder.Property(e => e.Amount).HasColumnName("amount");
        builder.Property(e => e.Currency).HasColumnName("currency");
        builder.Property(e => e.Interval).HasColumnName("interval");
        builder.Property(e => e.IntervalCount).HasColumnName("interval_count");
        builder.Property(e => e.MaxUsers).HasColumnName("max_users");
        builder.Property(e => e.MaxVehicles).HasColumnName("max_vehicles");
        builder.Property(e => e.MaxBookingsPerMonth).HasColumnName("max_bookings_per_month");
        builder.Property(e => e.Features).HasColumnName("features");
        builder.Property(e => e.IsActive).HasColumnName("is_active");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.Property(e => e.Features).HasColumnType("jsonb");
    }
}