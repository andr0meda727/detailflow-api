namespace Identity.Contracts;

// Integration events published by the Identity module. They carry everything a consumer needs,
// so consumers don't call back into Identity, and they stay plain records so they can travel over a broker.

/// <summary>
/// Someone was invited to join a studio. When <paramref name="HasAccount"/> is false the person has no login yet,
/// so the invitation goes out as an account invite; otherwise it is a normal email with the accept link.
/// </summary>
public sealed record EmployeeInvited(
    Guid StudioId,
    Guid InvitationId,
    string Email,
    string StudioName,
    string AcceptUrl,
    DateTime ExpiresAt,
    bool HasAccount);
