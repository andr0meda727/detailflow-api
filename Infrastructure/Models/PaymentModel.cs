using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("payment")]
public class PaymentModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("studio_id")]
    public Guid StudioId { get; set; }
    
    [Reference(typeof(StudioModel))]
    public StudioModel Studio { get; set; }
    
    [Column("subscription_id")]
    public Guid? SubscriptionId { get; set; }
    
    [Reference(typeof(StudioSubscriptionModel))]
    public StudioSubscriptionModel? Subscription { get; set; }
    
    [Column("stripe_payment_intent_id")]
    public string? StripePaymentIntentId { get; set; }

    [Column("stripe_invoice_id")]
    public string? StripeInvoiceId { get; set; }

    [Column("stripe_charge_id")]
    public string? StripeChargeId { get; set; }

    [Column("amount")]
    public long Amount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "pln";

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("payment_method_type")]
    public string? PaymentMethodType { get; set; }

    [Column("failure_code")]
    public string? FailureCode { get; set; }

    [Column("failure_message")]
    public string? FailureMessage { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}