using Core.Constants;
using Core.DTOs.Auth;
using Core.DTOs.Results;
using Core.DTOs.Services;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;

namespace Core.Services;

public class OwnerRegistrationService(
    IAuthService authService,
    IStudioRepository studioRepository,
    IStudioMemberRepository studioMemberRepository) : IOwnerRegistrationService
{
    public async Task<Result> RegisterAsync(RegisterOwnerDto dto)
    {
        var exists = await authService.UserExistsAsync(dto.Email);
        if (exists)
            return new Result(false, "EMAIL_TAKEN",
                "Konto z tym adresem e-mail już istnieje.");

        var userId = await authService.SignUpAsync(
            dto.Email, dto.Password, dto.FirstName, dto.LastName);

        var studio = await studioRepository.AddAsync(new Studio
        {
            Id      = Guid.NewGuid(),
            Name    = dto.Studio.Name,
            Phone   = dto.Studio.Phone,
            Email   = dto.Studio.Email,
            Address = dto.Studio.Address
        });

        await studioMemberRepository.AddAsync(new StudioMember
        {
            UserId  = userId,
            StudioId = studio.Id,
            RoleId  = Roles.Owner
        });

        return new Result(true, null, null);
    }
}