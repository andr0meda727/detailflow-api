namespace Core.Models;

public class SubscriptionPlan
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }

    public string StripeProductId { get; set; } = null!;

    public string StripePriceId { get; set; } = null!;

    public long Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string Interval { get; set; } = null!;

    public int IntervalCount { get; set; }

    public int? MaxUsers { get; set; }

    public int? MaxVehicles { get; set; }

    public int? MaxBookingsPerMonth { get; set; }

    public string? Features { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<StudioSubscription> StudioSubscriptions { get; set; } = new List<StudioSubscription>();
}