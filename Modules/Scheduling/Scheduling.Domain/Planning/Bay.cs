using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Scheduling.Domain.Planning;

/// <summary>
/// A place in the studio where one car is worked on at a time, e.g. "Stanowisko 1" or "Kabina lakiernicza".
/// Bookings occupy a bay for their whole visit, and no two open bookings may overlap on one bay. Deactivated bays
/// stay on past bookings but take no new ones.
/// </summary>
public class Bay : IAuditableEntity, ITenantEntity
{
    public const int MaxNameLength = 100;

    // Keeps the planner's input small; no detailing studio has more
    public const int MaxActivePerStudio = 20;

    // For EF Core
    private Bay() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public string Name { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static Bay Create(string name)
    {
        var bay = new Bay { Id = Guid.NewGuid(), IsActive = true };
        bay.Rename(name);
        return bay;
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > MaxNameLength) throw new ArgumentException("The name is too long.", nameof(name));

        Name = name.Trim();
    }

    public void Deactivate() => IsActive = false;
}
