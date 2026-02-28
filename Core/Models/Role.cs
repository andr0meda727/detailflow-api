namespace Core.Models;

public class Role
{
    public long Id { get; set; }

    public string RoleName { get; set; } = null!;

    public ICollection<StudioMember> StudioMembers { get; set; } = new List<StudioMember>();
}