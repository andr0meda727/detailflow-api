using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Core.Models;

public class Booking : IAuditableEntity, ITenantEntity
{
    public Guid Id { get; set; }

    public Guid StudioId { get; set; }

    public Guid VehicleId { get; set; }

    public Guid ClientId { get; set; }

    // Copies of the CRM client and vehicle, taken when the booking is saved and kept up to date from CRM events.
    // Bookings never join CRM tables, so CRM can live in another schema or service.
    public string ClientName { get; set; } = null!;

    public string ClientPhone { get; set; } = null!;

    public string? ClientEmail { get; set; }

    public string VehicleLicensePlate { get; set; } = null!;

    public string? VehicleBrand { get; set; }

    public string? VehicleModel { get; set; }

    public DateTime ScheduledAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid StatusId { get; set; }

    public long? TotalPrice { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<BookingServices> BookingServices { get; set; } = new List<BookingServices>();

    public User? CreatedByNavigation { get; set; }

    public BookingStatus Status { get; set; } = null!;

    public Studio Studio { get; set; } = null!;
}