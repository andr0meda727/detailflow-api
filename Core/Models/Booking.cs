using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Core.Models;

public class Booking : IAuditableEntity, ITenantEntity
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

    public ICollection<BookingServices> BookingServices { get; set; } = new List<BookingServices>();

    public Client Client { get; set; } = null!;

    public User? CreatedByNavigation { get; set; }

    public BookingStatus Status { get; set; } = null!;

    public Studio Studio { get; set; } = null!;

    public Vehicle Vehicle { get; set; } = null!;
}