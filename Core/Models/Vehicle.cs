using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Core.Models;

public class Vehicle : IAuditableEntity, ITenantEntity
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public Guid StudioId { get; set; }

    public string? Brand { get; set; }

    public string? Model { get; set; }

    public string LicensePlate { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public Client Client { get; set; } = null!;

    public Studio Studio { get; set; } = null!;
}