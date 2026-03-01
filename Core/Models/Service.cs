using Core.Interfaces.Audit;

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

    public bool IsActive { get; set; }

    public ICollection<BookingServices> BookingServices { get; set; } = new List<BookingServices>();

    public Studio? Studio { get; set; }
}