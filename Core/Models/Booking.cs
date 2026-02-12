namespace Core.Models;

public class Booking
{
    public Guid Id { get; set; }

    public Guid StudioId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid ClientId { get; set; }
    public Guid StatusId { get; set; }

    public DateTime ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? TotalPrice { get; set; }
    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Relationships
    public Studio? Studio { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Client? Client { get; set; }
    public BookingStatus? Status { get; set; }
    public User? CreatedByUser { get; set; }

    public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
}