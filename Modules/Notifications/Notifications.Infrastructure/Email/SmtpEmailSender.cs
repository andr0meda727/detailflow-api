using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Notifications.Infrastructure.Email;

/// <summary>
/// Sends the app's emails over SMTP (Mailpit locally, see compose.yaml). The content comes from <see cref="EmailTemplates"/>.
/// </summary>
public class SmtpEmailSender(IConfiguration config) : IEmailSender
{
    private readonly string _host = config["Email:Host"]
        ?? throw new InvalidOperationException("Brak Email:Host");

    private readonly int _port = int.Parse(config["Email:Port"] ?? "1025");

    private readonly string _from = config["Email:From"]
        ?? throw new InvalidOperationException("Brak Email:From");

    private readonly string _fromName = config["Email:FromName"] ?? "DetailFlow";

    public async Task SendInvitationEmailAsync(
        string to, string studioName, string acceptUrl, DateTime expiresAt, TimeZoneInfo timeZone)
    {
        using var message = Compose(to, EmailTemplates.Invitation(studioName, acceptUrl, expiresAt, timeZone));

        await SendAsync(message);
    }

    public async Task SendReportEmailAsync(string to, DateOnly dateFrom, DateOnly dateTo, byte[] pdfBytes)
    {
        using var message = Compose(to, EmailTemplates.BookingsReport(dateFrom, dateTo));
        message.Attachments.Add(new Attachment(
            new MemoryStream(pdfBytes), $"raport_{dateFrom:yyyyMMdd}_{dateTo:yyyyMMdd}.pdf", "application/pdf"));

        await SendAsync(message);
    }

    public async Task SendAsync(string to, EmailContent content)
    {
        using var message = Compose(to, content);

        await SendAsync(message);
    }

    private MailMessage Compose(string to, EmailContent content)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_from, _fromName),
            Subject = content.Subject,
            Body = content.Html,
            IsBodyHtml = true
        };
        message.To.Add(to);
        return message;
    }

    private async Task SendAsync(MailMessage message)
    {
        using var client = new SmtpClient(_host, _port);
        client.EnableSsl = false;
        client.Credentials = CredentialCache.DefaultNetworkCredentials;

        await client.SendMailAsync(message);
    }
}
