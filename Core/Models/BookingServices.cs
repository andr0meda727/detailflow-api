using Core.Interfaces.Audit;

namespace Core.Models;

public class BookingServices : IAuditableEntity
{
    public Guid BookingId { get; set; }

    public Guid ServiceId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Booking Booking { get; set; } = null!;

    public Service Service { get; set; } = null!;
}