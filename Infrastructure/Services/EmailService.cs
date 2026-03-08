using System.Net;
using System.Net.Mail;
using Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class EmailService(IConfiguration config) : IEmailService
{
    private readonly string _host = config["Email:Host"]
        ?? throw new InvalidOperationException("Brak Email:Host");

    private readonly int _port = int.Parse(config["Email:Port"] ?? "1025");

    private readonly string _from = config["Email:From"]
        ?? throw new InvalidOperationException("Brak Email:From");

    private readonly string _fromName = config["Email:FromName"] ?? "DetailFlow";

    public async Task SendInvitationEmailAsync(
        string to,
        string studioName,
        string acceptUrl,
        DateTime expiresAt)
    {
        using var client = new SmtpClient(_host, _port);
        client.EnableSsl = false;
        client.Credentials = CredentialCache.DefaultNetworkCredentials;

        var body = BuildInvitationHtml(studioName, acceptUrl, expiresAt);

        var message = new MailMessage
        {
            From       = new MailAddress(_from, _fromName),
            Subject    = $"Zaproszenie do studia {studioName}",
            Body       = body,
            IsBodyHtml = true
        };
        message.To.Add(to);

        await client.SendMailAsync(message);
    }

    private static string BuildInvitationHtml(
        string studioName, string acceptUrl, DateTime expiresAt)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2>Zaproszenie do {studioName}</h2>
                <p>Zostałeś zaproszony do dołączenia do studia <strong>{studioName}</strong> w aplikacji DetailFlow.</p>
                <p>Kliknij przycisk poniżej aby zaakceptować zaproszenie:</p>
                <a href="{acceptUrl}"
                   style="display: inline-block; padding: 12px 24px; background-color: #2563eb;
                          color: white; text-decoration: none; border-radius: 6px;">
                    Akceptuj zaproszenie
                </a>
                <p style="color: #6b7280; font-size: 14px; margin-top: 24px;">
                    Zaproszenie wygasa: {expiresAt:dd.MM.yyyy HH:mm} UTC
                </p>
                <p style="color: #6b7280; font-size: 12px;">
                    Jeśli nie spodziewałeś się tego zaproszenia, możesz zignorować tę wiadomość.
                </p>
            </body>
            </html>
            """;
    }

    public async Task SendReportEmailAsync(
        string to, DateTime dateFrom, DateTime dateTo, byte[] pdfBytes)
    {
        using var client = new SmtpClient(_host, _port);
        client.EnableSsl = false;
        client.Credentials = CredentialCache.DefaultNetworkCredentials;

        var message = new MailMessage
        {
            From       = new MailAddress(_from, _fromName),
            Subject    = $"Raport wizyt {dateFrom:dd.MM.yyyy}–{dateTo:dd.MM.yyyy}",
            Body       = $"""
                          <p>Cześć,</p>
                          <p>Twój raport wizyt za okres <strong>{dateFrom:dd.MM.yyyy} — {dateTo:dd.MM.yyyy}</strong> jest gotowy.</p>
                          <p>Znajdziesz go w załączniku.</p>
                          """,
            IsBodyHtml = true
        };

        message.To.Add(to);

        var attachment = new Attachment(
            new MemoryStream(pdfBytes),
            $"raport_{dateFrom:yyyyMMdd}_{dateTo:yyyyMMdd}.pdf",
            "application/pdf");

        message.Attachments.Add(attachment);

        await client.SendMailAsync(message);
    }
}