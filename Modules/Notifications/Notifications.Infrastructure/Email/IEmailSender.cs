namespace Notifications.Infrastructure.Email;

/// <summary>
/// The app's outgoing emails. <see cref="SmtpEmailSender"/> is the real one; tests use a fake.
/// </summary>
public interface IEmailSender
{
    /// <param name="expiresAt">UTC; the email shows it in <paramref name="timeZone"/>.</param>
    Task SendInvitationEmailAsync(string to, string studioName, string acceptUrl, DateTime expiresAt, TimeZoneInfo timeZone);

    Task SendReportEmailAsync(string to, DateOnly dateFrom, DateOnly dateTo, byte[] pdfBytes);

    /// <summary>
    /// Sends a prepared message (see <see cref="EmailTemplates"/>).
    /// </summary>
    Task SendAsync(string to, EmailContent content);
}
