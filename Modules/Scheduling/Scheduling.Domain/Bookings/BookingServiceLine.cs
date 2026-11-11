using Scheduling.Domain.Catalog;
using Shared.Kernel.Auditing;

namespace Scheduling.Domain.Bookings;

public class BookingServiceLine : IAuditableEntity
{
    // For EF Core
    private BookingServiceLine() { }

    internal BookingServiceLine(Guid bookingId, Guid serviceId)
    {
        BookingId = bookingId;
        ServiceId = serviceId;
    }

    public Guid BookingId { get; private set; }

    public Guid ServiceId { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Service Service { get; private set; } = null!;
}
