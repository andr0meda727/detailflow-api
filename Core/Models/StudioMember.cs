namespace Core.Models;

public class StudioMember
{
    public Guid UserId { get; set; }

    public Guid StudioId { get; set; }

    public long RoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Role Role { get; set; } = null!;

    public Studio Studio { get; set; } = null!;

    public User User { get; set; } = null!;
}