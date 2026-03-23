using Core.Interfaces.Audit;

namespace Core.Models;

public class StudioMember : IAuditableEntity
{
    public Guid UserId { get; set; }

    public Guid StudioId { get; set; }

    public long RoleId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Role Role { get; set; } = null!;

    public Studio Studio { get; set; } = null!;

    public User User { get; set; } = null!;
}