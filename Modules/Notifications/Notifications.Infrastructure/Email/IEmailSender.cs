namespace Notifications.Infrastructure.Email;

/// <summary>
/// The app's outgoing emails. <see cref="SmtpEmailSender"/> is the real one; tests use a fake.
/// </summary>
public interface IEmailSender
{
    Task SendInvitationEmailAsync(string to, string studioName, string acceptUrl, DateTime expiresAt);

    Task SendReportEmailAsync(string to, DateTime dateFrom, DateTime dateTo, byte[] pdfBytes);
}
