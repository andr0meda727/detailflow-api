namespace Core.Models;

public class Payment
{
    public Guid Id { get; set; }

    public Guid StudioId { get; set; }

    public Guid? SubscriptionId { get; set; }

    public string? StripePaymentIntentId { get; set; }

    public string? StripeInvoiceId { get; set; }

    public string? StripeChargeId { get; set; }

    public long Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? PaymentMethodType { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public StudioSubscription? Subscription { get; set; }
}