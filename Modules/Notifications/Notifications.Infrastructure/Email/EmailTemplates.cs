using System.Net;
using Shared.Kernel.Time;

namespace Notifications.Infrastructure.Email;

public sealed record EmailContent(string Subject, string Html);

/// <summary>
/// Subjects and HTML bodies of the app's emails. Anything a user typed (a studio name) is HTML-encoded:
/// the invitation goes to any address the owner enters, so unencoded markup would let them send links
/// of their choice from our domain.
/// </summary>
public static class EmailTemplates
{
    public static EmailContent Invitation(string studioName, string acceptUrl, DateTime expiresAt, TimeZoneInfo timeZone)
    {
        var name = WebUtility.HtmlEncode(studioName);
        var url = WebUtility.HtmlEncode(acceptUrl);
        var expires = TimeZones.ToLocal(expiresAt, timeZone);

        var html = $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2>Zaproszenie do {name}</h2>
                <p>Zostałeś zaproszony do dołączenia do studia <strong>{name}</strong> w aplikacji DetailFlow.</p>
                <p>Kliknij przycisk poniżej aby zaakceptować zaproszenie:</p>
                <a href="{url}"
                   style="display: inline-block; padding: 12px 24px; background-color: #2563eb;
                          color: white; text-decoration: none; border-radius: 6px;">
                    Akceptuj zaproszenie
                </a>
                <p style="color: #6b7280; font-size: 14px; margin-top: 24px;">
                    Zaproszenie wygasa: {expires:dd.MM.yyyy HH:mm} ({WebUtility.HtmlEncode(timeZone.Id)})
                </p>
                <p style="color: #6b7280; font-size: 12px;">
                    Jeśli nie spodziewałeś się tego zaproszenia, możesz zignorować tę wiadomość.
                </p>
            </body>
            </html>
            """;

        return new EmailContent($"Zaproszenie do studia {SingleLine(studioName)}", html);
    }

    public static EmailContent BookingsReport(DateTime dateFrom, DateTime dateTo) => new(
        $"Raport wizyt {dateFrom:dd.MM.yyyy}–{dateTo:dd.MM.yyyy}",
        $"""
         <p>Cześć,</p>
         <p>Twój raport wizyt za okres <strong>{dateFrom:dd.MM.yyyy} — {dateTo:dd.MM.yyyy}</strong> jest gotowy.</p>
         <p>Znajdziesz go w załączniku.</p>
         """);

    // A subject can't hold line breaks (MailMessage throws on them)
    private static string SingleLine(string text) =>
        string.Join(' ', text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
