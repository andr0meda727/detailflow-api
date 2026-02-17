namespace Core.Models;

public class BookingServices
{
    public Guid BookingId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Relationships
    public Booking? Booking { get; set; }
    public Service? Service { get; set; }
}