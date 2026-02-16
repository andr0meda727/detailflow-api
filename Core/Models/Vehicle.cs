namespace Core.Models;

public class Vehicle
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid StudioId { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Relationships
    public Client? Client { get; set; }
    public Studio? Studio { get; set; }
    public ICollection<Booking> Bookings { get; init; } = new List<Booking>();
}