using Core.DTOs.Studio;

namespace Core.Interfaces.Services;

public interface IStudioService
{
    Task<List<UserStudioDto>> GetStudiosForUserAsync(Guid userId);
}