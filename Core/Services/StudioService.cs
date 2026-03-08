using Core.DTOs.Studio;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Core.Services;

public class StudioService(IStudioMemberRepository studioMemberRepository) : IStudioService
{
    public async Task<List<UserStudioDto>> GetStudiosForUserAsync(Guid userId)
    {
        var memberships = await studioMemberRepository.GetByUserIdWithStudioAsync(userId);

        return memberships.Select(m => new UserStudioDto(
            m.StudioId,
            m.Studio.Name,
            m.Role.RoleName,
            m.Studio.Address
        )).ToList();
    }
}