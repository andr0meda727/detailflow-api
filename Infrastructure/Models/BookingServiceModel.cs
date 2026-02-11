using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("booking_service")]
public class BookingServiceModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("booking_id")]
    public Guid BookingId { get; set; }
    
    [Reference(typeof(BookingModel))]
    public BookingModel Booking { get; set; }
    
    [Column("service_id")]
    public Guid ServiceId { get; set; }
    
    [Reference(typeof(ServiceModel))]
    public ServiceModel Service { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}