namespace Core.Interfaces.Services;

public interface IEmailService
{
    Task SendInvitationEmailAsync(string to, string studioName, string acceptUrl, DateTime expiresAt);
    Task SendReportEmailAsync(string to, DateTime dateFrom, DateTime dateTo, byte[] pdfBytes);
}