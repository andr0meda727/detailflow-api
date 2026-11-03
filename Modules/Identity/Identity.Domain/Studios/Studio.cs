using Shared.Kernel.Auditing;
using Shared.Kernel.Results;

namespace Identity.Domain.Studios;

/// <summary>
/// A detailing studio, the tenant of the system, together with the people who work in it.
/// </summary>
public class Studio : IAuditableEntity
{
    private readonly List<StudioMember> _members = [];

    // For EF Core
    private Studio() { }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Address { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyCollection<StudioMember> Members => _members;

    /// <summary>
    /// Creates a studio with its owner, who is a member from the start.
    /// </summary>
    public static Studio Register(string name, string? phone, string? email, string? address, Guid ownerUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var studio = new Studio
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Phone = Blank(phone),
            Email = Blank(email),
            Address = Blank(address)
        };
        studio._members.Add(new StudioMember(studio.Id, ownerUserId, Roles.Owner));
        return studio;
    }

    public StudioMember? FindMember(Guid userId) => _members.FirstOrDefault(m => m.UserId == userId);

    /// <summary>
    /// Adds a person with the given role, or brings back someone who was removed earlier.
    /// </summary>
    public void Admit(Guid userId, long roleId)
    {
        if (!Roles.IsAssignable(roleId)) throw new ArgumentOutOfRangeException(nameof(roleId), roleId, "Not an assignable role.");

        var member = FindMember(userId);
        if (member == null)
        {
            _members.Add(new StudioMember(Id, userId, roleId));
        }
        else if (!member.IsActive)
        {
            member.Reactivate(roleId);
        }
    }

    public Result RemoveMember(Guid userId, Guid requestedBy)
    {
        if (userId == requestedBy) return StudioErrors.SelfRemove;

        var member = FindMember(userId);
        if (member is not { IsActive: true }) return StudioErrors.MemberNotFound;
        if (member.RoleId == Roles.Owner) return StudioErrors.CannotRemoveOwner;

        member.Deactivate();
        return Result.Ok();
    }

    public Result ChangeRole(Guid userId, long roleId, Guid requestedBy)
    {
        if (userId == requestedBy) return StudioErrors.SelfRoleChange;

        var member = FindMember(userId);
        if (member is not { IsActive: true }) return StudioErrors.MemberNotFound;
        if (member.RoleId == Roles.Owner) return StudioErrors.CannotChangeOwnerRole;
        if (!Roles.IsAssignable(roleId)) return StudioErrors.InvalidRole;

        member.ChangeRole(roleId);
        return Result.Ok();
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public class StudioMember : IAuditableEntity
{
    // For EF Core
    private StudioMember() { }

    internal StudioMember(Guid studioId, Guid userId, long roleId)
    {
        StudioId = studioId;
        UserId = userId;
        RoleId = roleId;
        IsActive = true;
    }

    public Guid StudioId { get; private set; }

    public Guid UserId { get; private set; }

    public long RoleId { get; private set; }

    // Removed members are kept, inactive, so they can be brought back and their history stays readable
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    internal void ChangeRole(long roleId) => RoleId = roleId;

    internal void Deactivate() => IsActive = false;

    internal void Reactivate(long roleId)
    {
        RoleId = roleId;
        IsActive = true;
    }
}

public static class StudioErrors
{
    public static readonly Error SelfRemove = Error.Validation("SELF_REMOVE", "Nie możesz zwolnić samego siebie.");

    public static readonly Error SelfRoleChange = Error.Validation("SELF_ROLE_CHANGE", "Nie możesz zmienić własnej roli.");

    public static readonly Error MemberNotFound =
        Error.NotFound("NOT_FOUND", "Nie znaleziono aktywnego pracownika w tym studiu.");

    public static readonly Error CannotRemoveOwner =
        Error.Conflict("CANNOT_REMOVE_OWNER", "Nie można usunąć właściciela studia.");

    public static readonly Error CannotChangeOwnerRole =
        Error.Conflict("CANNOT_CHANGE_OWNER_ROLE", "Nie można zmienić roli właściciela.");

    public static readonly Error InvalidRole = Error.Validation("INVALID_ROLE", "Podana rola jest nieprawidłowa.");
}
