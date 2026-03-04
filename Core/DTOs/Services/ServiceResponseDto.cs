namespace Core.DTOs.Services;

public record ServiceResponseDto(
    Guid Id,
    string Name,
    bool IsActive
);
