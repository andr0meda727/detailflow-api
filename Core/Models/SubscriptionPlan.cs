namespace Core.Models;

public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StripeProductId { get; set; } = string.Empty;
    public string StripePriceId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = "pln";
    public string Interval { get; set; } = string.Empty;
    public int IntervalCount { get; set; } = 1;
    public int? MaxUsers { get; set; }
    public int? MaxVehicles { get; set; }
    public int? MaxBookingsPerMonth { get; set; }
    public string? Features { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}