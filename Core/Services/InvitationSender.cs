using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;

namespace Core.Services;

public class InvitationSender(IAuthService authService, IEmailService emailService, IStudioRepository studioRepo) : IInvitationSender
{
    public async Task SendInvitationAsync(StudioInvitation invitation, bool userExists, Guid studioId, string acceptUrl)
    {
        if (!userExists)
        {
            await authService.InviteUserAsync(invitation.Email, redirectUrl: acceptUrl);
            return;
        }
        var studio = await studioRepo.GetByIdAsync(studioId);
        await emailService.SendInvitationEmailAsync(
            invitation.Email, studio?.Name ?? "DetailFlow Studio", acceptUrl, invitation.ExpiresAt);
    }
}