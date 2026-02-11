using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("studio_subscription")]
public class StudioSubscriptionModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("studio_id")]
    public Guid StudioId { get; set; }
    
    [Reference(typeof(StudioModel))]
    public StudioModel Studio { get; set; }
    
    [Column("plan_id")]
    public Guid PlanId { get; set; }
    
    [Reference(typeof(SubscriptionPlanModel))]
    public SubscriptionPlanModel Plan { get; set; }
    
    [Column("stripe_subscription_id")]
    public string StripeSubscriptionId { get; set; } = string.Empty;

    [Column("stripe_status")]
    public string StripeStatus { get; set; } = string.Empty;

    [Column("current_period_start")]
    public DateTime CurrentPeriodStart { get; set; }

    [Column("current_period_end")]
    public DateTime CurrentPeriodEnd { get; set; }

    [Column("cancel_at_period_end")]
    public bool CancelAtPeriodEnd { get; set; }

    [Column("canceled_at")]
    public DateTime? CanceledAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [Column("trial_start")]
    public DateTime? TrialStart { get; set; }

    [Column("trial_end")]
    public DateTime? TrialEnd { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}