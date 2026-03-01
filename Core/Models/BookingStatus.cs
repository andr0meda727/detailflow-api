namespace Core.Models;

public class BookingStatus
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Color { get; set; }

    public int SortOrder { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}