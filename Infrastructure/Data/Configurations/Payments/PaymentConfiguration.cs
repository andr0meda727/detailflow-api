using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Payments;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment");
        builder.HasKey(x => x.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
        builder.Property(e => e.StripePaymentIntentId).HasColumnName("stripe_payment_intent_id");
        builder.Property(e => e.StripeInvoiceId).HasColumnName("stripe_invoice_id");
        builder.Property(e => e.StripeChargeId).HasColumnName("stripe_charge_id");
        builder.Property(e => e.Amount).HasColumnName("amount");
        builder.Property(e => e.Currency).HasColumnName("currency");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.PaymentMethodType).HasColumnName("payment_method_type");
        builder.Property(e => e.FailureCode).HasColumnName("failure_code");
        builder.Property(e => e.FailureMessage).HasColumnName("failure_message");
        builder.Property(e => e.PaidAt).HasColumnName("paid_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        builder.HasOne(x => x.Studio)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Subscription)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}