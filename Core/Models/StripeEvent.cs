namespace Core.Models;

public class StripeEvent
{
    public Guid Id { get; set; }

    public string StripeEventId { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public Guid? StudioId { get; set; }

    public Guid? SubscriptionId { get; set; }

    public string Payload { get; set; } = null!;

    public bool Processed { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public StudioSubscription? Subscription { get; set; }
}