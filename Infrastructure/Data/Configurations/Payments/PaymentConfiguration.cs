using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Payments;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("payment_pkey");

        builder
            .ToTable("payment", "d");

        builder
            .HasIndex(e => e.StripePaymentIntentId, "payment_stripe_payment_intent_id_key")
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

        builder.Property(e => e.FailureCode)
            .HasColumnName("failure_code");

        builder.Property(e => e.FailureMessage)
            .HasColumnName("failure_message");

        builder.Property(e => e.PaidAt)
            .HasColumnName("paid_at");

        builder.Property(e => e.PaymentMethodType)
            .HasColumnName("payment_method_type");

        builder.Property(e => e.Status)
            .HasColumnName("status");

        builder.Property(e => e.StripeChargeId)
            .HasColumnName("stripe_charge_id");

        builder.Property(e => e.StripeInvoiceId)
            .HasColumnName("stripe_invoice_id");

        builder.Property(e => e.StripePaymentIntentId)
            .HasColumnName("stripe_payment_intent_id");

        builder.Property(e => e.StudioId)
            .HasColumnName("studio_id");

        builder.Property(e => e.SubscriptionId)
            .HasColumnName("subscription_id");

        // The studio belongs to the Identity module: a plain ID here; the database foreign key stays
        builder.HasIndex(e => e.StudioId);

        builder
            .HasOne(d => d.Subscription)
            .WithMany(p => p.Payments)
            .HasForeignKey(d => d.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("payment_subscription_id_fkey");
    }
}