namespace Core.DTOs.Results;

public record Result(bool Success, string? ErrorCode, string? ErrorMessage)
{
    public static Result Ok() => new(true, null, null);
    public static Result Fail(string code, string message) => new(false, code, message);
}