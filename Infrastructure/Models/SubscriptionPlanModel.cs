using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("subscription_plan")]
public class SubscriptionPlanModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("stripe_product_id")]
    public string StripeProductId { get; set; } = string.Empty;

    [Column("stripe_price_id")]
    public string StripePriceId { get; set; } = string.Empty;

    [Column("amount")]
    public long Amount { get; set; } // bigint -> long

    [Column("currency")]
    public string Currency { get; set; } = "pln";

    [Column("interval")]
    public string Interval { get; set; } = string.Empty;

    [Column("interval_count")]
    public int IntervalCount { get; set; } = 1;

    [Column("max_users")]
    public int? MaxUsers { get; set; }

    [Column("max_vehicles")]
    public int? MaxVehicles { get; set; }

    [Column("max_bookings_per_month")]
    public int? MaxBookingsPerMonth { get; set; }
    
    [Column("features")]
    public string? Features { get; set; } 

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}