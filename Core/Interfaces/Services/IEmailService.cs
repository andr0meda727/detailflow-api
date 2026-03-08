namespace Core.Interfaces.Services;

public interface IEmailService
{
    Task SendInvitationEmailAsync(string to, string studioName, string acceptUrl, DateTime expiresAt);
}