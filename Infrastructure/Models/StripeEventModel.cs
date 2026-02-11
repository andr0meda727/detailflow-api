using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("stripe_event")]
public class StripeEventModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("stripe_event_id")]
    public string StripeEventId { get; set; } = string.Empty;

    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;
    
    [Column("studio_id")]
    public Guid? StudioId { get; set; }
    
    [Reference(typeof(StudioModel))]
    public StudioModel? Studio { get; set; }
    
    [Column("subscription_id")]
    public Guid? SubscriptionId { get; set; }
    
    [Reference(typeof(StudioSubscriptionModel))]
    public StudioSubscriptionModel? Subscription { get; set; }
    
    [Column("payload")]
    public string Payload { get; set; } = string.Empty;

    [Column("processed")]
    public bool Processed { get; set; }

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}