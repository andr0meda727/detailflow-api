namespace Core.DTOs.Results;

public record InviteResult(
    bool Success,
    Guid? InvitationId,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static InviteResult Ok(Guid id) =>
        new(true, id, null, null);

    public static InviteResult Fail(string code, string message) =>
        new(false, null, code, message);
}