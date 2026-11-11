using Shared.Kernel.Auditing;

namespace Identity.Domain.Users;

/// <summary>
/// The application profile of a Supabase account; the ID is the Supabase user ID.
/// Rows are created by the database when the Supabase account is created, not by this code.
/// </summary>
public class User : IAuditableEntity
{
    // For EF Core
    private User() { }

    public User(Guid id, string email, string? firstName, string? lastName)
    {
        Id = id;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool HasEmail(string email) => string.Equals(Email, email.Trim(), StringComparison.OrdinalIgnoreCase);
}
