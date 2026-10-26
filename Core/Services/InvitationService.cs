using Core.Constants;
using Core.DTOs.Employees;
using Core.DTOs.Results;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;

namespace Core.Services;

public class InvitationService(
    IStudioInvitationRepository invitationRepository,
    IStudioMemberRepository studioMemberRepository,
    IUserRepository userRepository) : IInvitationService
{
    public async Task<InvitationResponseDto?> GetByTokenAsync(Guid token)
    {
        var invitation = await invitationRepository.GetByTokenAsync(token);

        if (invitation == null || invitation.Status != InvitationStatus.Pending)
        {
            return null;
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await invitationRepository.UpdateAsync(invitation);
            return null;
        }

        return new InvitationResponseDto(
            invitation.Id,
            invitation.Studio.Name,
            invitation.Role.RoleName,
            invitation.Email,
            invitation.ExpiresAt
        );
    }

    public async Task<Result> AcceptAsync(Guid token, Guid userId)
    {
        var invitation = await invitationRepository.GetByTokenAsync(token);

        if (invitation == null)
            return Error.NotFound("NOT_FOUND", "Zaproszenie nie istnieje.");

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await invitationRepository.UpdateAsync(invitation);
            return Error.Conflict("EXPIRED",
                "Zaproszenie wygasło. Poproś właściciela o ponowne wysłanie.");
        }

        if (invitation.Status != InvitationStatus.Pending)
            return Error.Conflict("ALREADY_USED",
                invitation.Status == InvitationStatus.Accepted
                    ? "To zaproszenie zostało już zaakceptowane."
                    : "To zaproszenie zostało już odrzucone.");


        var user = await userRepository.GetByIdAsync(userId);
        if (user?.Email.ToLower() != invitation.Email.ToLower())
            return Error.Forbidden("WRONG_USER",
                "To zaproszenie zostało wysłane na inny adres e-mail");


        var alreadyMember = await studioMemberRepository
            .GetByUserIdAndStudioAsync(userId, invitation.StudioId);

        if (alreadyMember != null)
        {
            invitation.Status = InvitationStatus.Accepted;
            await invitationRepository.UpdateAsync(invitation);
            return Result.Ok();
        }

        await studioMemberRepository.AddAsync(new StudioMember
        {
            UserId  = userId,
            StudioId = invitation.StudioId,
            RoleId  = invitation.RoleId,
            IsActive = true
        });

        invitation.Status = InvitationStatus.Accepted;
        await invitationRepository.UpdateAsync(invitation);

        return Result.Ok();
    }

    public async Task<Result> DeclineAsync(Guid token, Guid userId)
    {
        var invitation = await invitationRepository.GetByTokenAsync(token);

        if (invitation == null)
            return Error.NotFound("NOT_FOUND", "Zaproszenie nie istnieje.");

        if (invitation.Status != InvitationStatus.Pending)
            return Error.Conflict("ALREADY_USED", "To zaproszenie zostało już wykorzystane.");

        var user = await userRepository.GetByIdAsync(userId);
        if (user?.Email.ToLower() != invitation.Email.ToLower())
            return Error.Forbidden("WRONG_USER",
                "To zaproszenie zostało wysłane na inny adres e-mail.");

        invitation.Status = InvitationStatus.Declined;
        await invitationRepository.UpdateAsync(invitation);

        return Result.Ok();
    }
}