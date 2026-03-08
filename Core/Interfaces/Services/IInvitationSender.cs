using Core.Models;

namespace Core.Interfaces.Services;

public interface IInvitationSender
{
    Task SendInvitationAsync(StudioInvitation invitation, bool userExists, Guid studioId, string acceptUrl);
}