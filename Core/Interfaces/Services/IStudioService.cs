using Core.DTOs.Studio;

namespace Core.Interfaces.Services;

public interface IStudioService
{
    Task<List<UserStudioDto>> GetStudiosForUserAsync(Guid userId);
    Task<UserStudioDto> CreateForUserAsync(Guid userId, CreateStudioDto dto);
    Task<UserStudioDto?> UpdateAsync(Guid studioId, Guid userId, UpdateStudioDto dto);
}
