using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Core.Models;

public class User : IAuditableEntity
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<StudioMember> StudioMembers { get; set; } = new List<StudioMember>();
}