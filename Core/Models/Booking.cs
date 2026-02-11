namespace Core.Models;

public class Booking
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid StatusId { get; set; }
    public long? TotalPrice { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}