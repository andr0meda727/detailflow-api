namespace Identity.Domain.Studios;

/// <summary>
/// Well-known rows of the role table. Behaviour keys off these IDs; the table holds the display names.
/// </summary>
public static class Roles
{
    public const long Owner = 1;
    public const long Manager = 2;
    public const long Employee = 3;

    // The owner role comes with the studio and can't be handed out or taken away
    public static bool IsAssignable(long roleId) => roleId is Manager or Employee;
}

public class Role
{
    // For EF Core
    private Role() { }

    public Role(long id, string name)
    {
        Id = id;
        Name = name;
    }

    public long Id { get; private set; }

    public string Name { get; private set; } = null!;
}
