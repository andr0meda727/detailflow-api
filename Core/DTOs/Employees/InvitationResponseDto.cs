namespace Core.DTOs.Employees;

public record InvitationResponseDto(
    Guid Id,
    string StudioName,
    string Role,
    string Email,
    DateTime ExpiresAt
);