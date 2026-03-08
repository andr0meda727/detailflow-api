namespace Core.DTOs.Results;

public record AcceptResult(bool Success, string? ErrorCode, string? ErrorMessage)
{
    public static AcceptResult Ok() => new(true, null, null);
    public static AcceptResult Fail(string code, string message) => new(false, code, message);
}