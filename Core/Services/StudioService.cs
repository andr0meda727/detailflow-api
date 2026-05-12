using Core.Constants;
using Core.DTOs.Studio;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;

namespace Core.Services;

public class StudioService(
    IStudioRepository studioRepository,
    IStudioMemberRepository studioMemberRepository) : IStudioService
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

    public async Task<UserStudioDto> CreateForUserAsync(Guid userId, CreateStudioDto dto)
    {
        var studio = await studioRepository.AddAsync(new Studio
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address
        });

        await studioMemberRepository.AddAsync(new StudioMember
        {
            UserId = userId,
            StudioId = studio.Id,
            RoleId = Roles.Owner,
            IsActive = true
        });

        return new UserStudioDto(studio.Id, studio.Name, nameof(Roles.Owner), studio.Address);
    }

    public async Task<UserStudioDto?> UpdateAsync(Guid studioId, Guid userId, UpdateStudioDto dto)
    {
        var membership = await studioMemberRepository.GetByUserIdAndStudioAsync(userId, studioId);
        if (membership is not { IsActive: true, RoleId: Roles.Owner })
        {
            return null;
        }

        var studio = await studioRepository.GetByIdAsync(studioId);
        if (studio == null)
        {
            return null;
        }

        studio.Name = dto.Name;
        studio.Phone = dto.Phone;
        studio.Email = dto.Email;
        studio.Address = dto.Address;

        await studioRepository.UpdateAsync(studio);

        return new UserStudioDto(studio.Id, studio.Name, nameof(Roles.Owner), studio.Address);
    }
}
