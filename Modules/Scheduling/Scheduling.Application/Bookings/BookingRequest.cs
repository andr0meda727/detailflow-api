namespace Scheduling.Application.Bookings;

public class BookingRequest
{
    public Guid ClientId { get; set; }

    public Guid VehicleId { get; set; }

    public DateTime ScheduledAt { get; set; }

    // Created keeps the booking open; Completed or Cancelled finishes it right away
    public Guid StatusId { get; set; }

    public List<Guid> ServiceIds { get; set; } = [];

    public decimal Price { get; set; }

    public string? Notes { get; set; }

    // Puts the car on this bay; left out, a studio that plans picks the best-fitting free bay
    public Guid? BayId { get; set; }

    public long PriceInCents => (long)Math.Round(Price * 100);
}
