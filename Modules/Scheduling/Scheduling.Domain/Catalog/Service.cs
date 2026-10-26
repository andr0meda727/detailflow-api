using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Scheduling.Domain.Catalog;

/// <summary>
/// Something a studio offers and books, e.g. "Polishing". Deactivated services stay on past bookings.
/// </summary>
public class Service : IAuditableEntity, ITenantEntity
{
    // For EF Core
    private Service() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public string Name { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static Service Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Service { Id = Guid.NewGuid(), Name = name.Trim(), IsActive = true };
    }

    public void Deactivate() => IsActive = false;
}
