namespace Core.DTOs.Bookings;

public class BookingCreateDto
{
    public Guid VehicleId { get; set; }
    
    public Guid ClientId { get; set; }
    
    public DateTime ScheduledAt { get; set; }
    
    public Guid StatusId { get; set; }
    
    public List<Guid> ServiceIds { get; set; }
    
    public decimal Price { get; set; }
    
    public string? Notes { get; set; }
}