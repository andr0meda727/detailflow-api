using Identity.Domain.Studios;
using Shared.Kernel.Auditing;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Identity.Domain.Invitations;

/// <summary>
/// An invitation for an email address to join a studio. The token in the link is the credential.
/// A studio has at most one invitation per email: inviting again after a decline or expiry reissues it.
/// </summary>
public class StudioInvitation : IAuditableEntity, ITenantEntity
{
    public static readonly TimeSpan ValidFor = TimeSpan.FromDays(7);

    // For EF Core
    private StudioInvitation() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public string Email { get; private set; } = null!;

    public long RoleId { get; private set; }

    public Guid Token { get; private set; }

    public string Status { get; private set; } = null!;

    public Guid InvitedBy { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static StudioInvitation Create(string email, long roleId, Guid invitedBy, DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var invitation = new StudioInvitation { Id = Guid.NewGuid(), Email = email.Trim() };
        invitation.Issue(roleId, invitedBy, now);
        return invitation;
    }

    /// <summary>
    /// Waiting for an answer and still valid.
    /// </summary>
    public bool IsOpen(DateTime now) => Status == InvitationStatus.Pending && ExpiresAt > now;

    /// <summary>
    /// Sends the invitation again with a new token, after it was declined, accepted earlier or expired.
    /// </summary>
    public void Reissue(long roleId, Guid invitedBy, DateTime now)
    {
        if (IsOpen(now)) throw new InvalidOperationException("The invitation is still open.");

        Issue(roleId, invitedBy, now);
    }

    /// <summary>
    /// Records that the time ran out, if it did. Returns whether the invitation is expired.
    /// </summary>
    public bool ExpireIfDue(DateTime now)
    {
        if (Status == InvitationStatus.Pending && ExpiresAt <= now) Status = InvitationStatus.Expired;
        return Status == InvitationStatus.Expired;
    }

    public Result Accept(string userEmail, DateTime now)
    {
        var check = CanAnswer(userEmail, now);
        if (!check.Success) return check;

        Status = InvitationStatus.Accepted;
        return Result.Ok();
    }

    public Result Decline(string userEmail, DateTime now)
    {
        var check = CanAnswer(userEmail, now);
        if (!check.Success) return check;

        Status = InvitationStatus.Declined;
        return Result.Ok();
    }

    private Result CanAnswer(string userEmail, DateTime now)
    {
        if (ExpireIfDue(now)) return InvitationErrors.Expired;
        if (Status != InvitationStatus.Pending) return InvitationErrors.AlreadyUsed(Status);
        if (!string.Equals(Email, userEmail.Trim(), StringComparison.OrdinalIgnoreCase)) return InvitationErrors.WrongUser;

        return Result.Ok();
    }

    private void Issue(long roleId, Guid invitedBy, DateTime now)
    {
        if (!Roles.IsAssignable(roleId)) throw new ArgumentOutOfRangeException(nameof(roleId), roleId, "Not an assignable role.");

        RoleId = roleId;
        InvitedBy = invitedBy;
        Token = Guid.NewGuid();
        Status = InvitationStatus.Pending;
        ExpiresAt = now.Add(ValidFor);
    }
}

public static class InvitationStatus
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Declined = "declined";
    public const string Expired = "expired";
}

public static class InvitationErrors
{
    public static readonly Error NotFound = Error.NotFound("NOT_FOUND", "Zaproszenie nie istnieje.");

    public static readonly Error Expired =
        Error.Conflict("EXPIRED", "Zaproszenie wygasło. Poproś właściciela o ponowne wysłanie.");

    public static readonly Error WrongUser =
        Error.Forbidden("WRONG_USER", "To zaproszenie zostało wysłane na inny adres e-mail.");

    public static Error AlreadyUsed(string status) => Error.Conflict("ALREADY_USED", status switch
    {
        InvitationStatus.Accepted => "To zaproszenie zostało już zaakceptowane.",
        InvitationStatus.Declined => "To zaproszenie zostało już odrzucone.",
        _ => "To zaproszenie zostało już wykorzystane."
    });
}
