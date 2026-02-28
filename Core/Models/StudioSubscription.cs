namespace Core.Models;

public class StudioSubscription
{
    public Guid Id { get; set; }

    public Guid StudioId { get; set; }

    public Guid PlanId { get; set; }

    public string StripeSubscriptionId { get; set; } = null!;

    public string StripeStatus { get; set; } = null!;

    public DateTime CurrentPeriodStart { get; set; }

    public DateTime CurrentPeriodEnd { get; set; }

    public bool CancelAtPeriodEnd { get; set; }

    public DateTime? CanceledAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public DateTime? TrialStart { get; set; }

    public DateTime? TrialEnd { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public SubscriptionPlan Plan { get; set; } = null!;

    public ICollection<StripeEvent> StripeEvents { get; set; } = new List<StripeEvent>();

    public Studio Studio { get; set; } = null!;
}