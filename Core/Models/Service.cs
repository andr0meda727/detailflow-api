using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Core.Models;

/// <summary>
/// Table contains services that studio can choose for booking purposes. The studio can add its own new services.
/// </summary>
public class Service : IAuditableEntity, ITenantEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid StudioId { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<BookingServices> BookingServices { get; set; } = new List<BookingServices>();

    public Studio? Studio { get; set; }
}