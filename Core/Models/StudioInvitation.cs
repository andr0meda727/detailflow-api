using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Core.Models;

public class StudioInvitation : IAuditableEntity, ITenantEntity
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public string Email { get; set; } = null!;
    public long RoleId { get; set; }
    public Guid Token { get; set; }
    public string Status { get; set; } = "pending";
    public Guid InvitedBy { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Studio Studio { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public User InvitedByUser { get; set; } = null!;
}