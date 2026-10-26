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
    IStudioRepository studioRepository) : IOwnerRegistrationService
{
    public async Task<Result> RegisterAsync(RegisterOwnerDto dto)
    {
        var exists = await authService.UserExistsAsync(dto.Email);
        if (exists)
            return new Result(false, "EMAIL_TAKEN",
                "Konto z tym adresem e-mail już istnieje.");

        var userId = await authService.SignUpAsync(
            dto.Email, dto.Password, dto.FirstName, dto.LastName);

        var studio = new Studio
        {
            Id      = Guid.NewGuid(),
            Name    = dto.Studio.Name,
            Phone   = dto.Studio.Phone,
            Email   = dto.Studio.Email,
            Address = dto.Studio.Address
        };

        // Added through the navigation so the studio and its owner are saved in one transaction
        studio.StudioMembers.Add(new StudioMember
        {
            UserId   = userId,
            RoleId   = Roles.Owner,
            IsActive = true
        });

        try
        {
            await studioRepository.AddAsync(studio);
        }
        catch
        {
            // Don't leave an auth account behind that has no studio and can't register again
            await authService.DeleteUserAsync(userId);
            throw;
        }

        return new Result(true, null, null);
    }
}