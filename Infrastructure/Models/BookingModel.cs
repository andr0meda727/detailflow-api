using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("booking")]
public class BookingModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("studio_id")]
    public Guid StudioId { get; set; }
    
    [Reference(typeof(StudioModel))]
    public StudioModel Studio { get; set; }
    
    [Column("vehicle_id")]
    public Guid VehicleId { get; set; }
    
    [Reference(typeof(VehicleModel))]
    public VehicleModel Vehicle { get; set; }
    
    [Column("client_id")]
    public Guid ClientId { get; set; }
    
    [Reference(typeof(ClientModel))]
    public ClientModel Client { get; set; }
    
    [Column("status_id")]
    public Guid StatusId { get; set; }
    
    [Reference(typeof(BookingStatusModel))]
    public BookingStatusModel Status { get; set; }
    
    [Column("created_by")]
    public Guid? CreatedBy { get; set; }
    
    [Reference(typeof(UserModel))]
    public UserModel? Creator { get; set; }
    
    [Column("scheduled_at")]
    public DateTime ScheduledAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("total_price")]
    public long? TotalPrice { get; set; } // bigint -> long

    [Column("notes")]
    public string? Notes { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}