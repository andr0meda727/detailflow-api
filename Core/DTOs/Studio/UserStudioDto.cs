namespace Core.DTOs.Studio;

public record UserStudioDto(Guid StudioId, string Name, string Role, string? Address);