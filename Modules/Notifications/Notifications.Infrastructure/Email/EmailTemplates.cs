using System.Text.Encodings.Web;
using System.Text.Unicode;
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
        var name = Html(studioName);
        var url = Html(acceptUrl);
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
                    Zaproszenie wygasa: {expires:dd.MM.yyyy HH:mm} ({Html(timeZone.Id)})
                </p>
                <p style="color: #6b7280; font-size: 12px;">
                    Jeśli nie spodziewałeś się tego zaproszenia, możesz zignorować tę wiadomość.
                </p>
            </body>
            </html>
            """;

        return new EmailContent($"Zaproszenie do studia {SingleLine(studioName)}", html);
    }

    public static EmailContent BookingsReport(DateOnly dateFrom, DateOnly dateTo) => new(
        $"Raport wizyt {dateFrom:dd.MM.yyyy}–{dateTo:dd.MM.yyyy}",
        $"""
         <p>Cześć,</p>
         <p>Twój raport wizyt za okres <strong>{dateFrom:dd.MM.yyyy} — {dateTo:dd.MM.yyyy}</strong> jest gotowy.</p>
         <p>Znajdziesz go w załączniku.</p>
         """);

    /// <summary>
    /// To the studio's owners: a customer asked for a visit and is waiting for an answer.
    /// </summary>
    public static EmailContent BookingRequestReceived(
        string customerName, string customerPhone, string licensePlate, IReadOnlyList<string> services,
        DateTime preferredAtUtc, TimeZoneInfo timeZone)
    {
        var when = TimeZones.ToLocal(preferredAtUtc, timeZone);
        var list = string.Join("", services.Select(s => $"<li>{Html(s)}</li>"));

        return new EmailContent(
            $"Nowa prośba o rezerwację: {SingleLine(customerName)}, {when:dd.MM.yyyy HH:mm}",
            $"""
             <p>Klient <strong>{Html(customerName)}</strong> (tel. {Html(customerPhone)})
             prosi o wizytę <strong>{when:dd.MM.yyyy HH:mm}</strong> ({Html(timeZone.Id)})
             dla pojazdu {Html(licensePlate)}.</p>
             <ul>{list}</ul>
             <p>Zaakceptuj albo odrzuć prośbę w panelu DetailFlow, w zakładce z prośbami o rezerwację.</p>
             """);
    }

    /// <summary>
    /// To the customer: the studio booked the visit.
    /// </summary>
    public static EmailContent BookingRequestAccepted(
        string customerName, string studioName, DateTime scheduledAtUtc, TimeZoneInfo timeZone)
    {
        var when = TimeZones.ToLocal(scheduledAtUtc, timeZone);

        return new EmailContent(
            $"{SingleLine(studioName)} potwierdza wizytę {when:dd.MM.yyyy HH:mm}",
            $"""
             <p>Cześć {Html(customerName)},</p>
             <p>Studio <strong>{Html(studioName)}</strong> przyjęło Twoją rezerwację na
             <strong>{when:dd.MM.yyyy HH:mm}</strong> ({Html(timeZone.Id)}).</p>
             <p>Szczegóły znajdziesz w historii rezerwacji na swoim koncie.</p>
             """);
    }

    /// <summary>
    /// To the customer: the studio can't take the visit, with its reason when it gave one.
    /// </summary>
    public static EmailContent BookingRequestRejected(string customerName, string studioName, string? reason)
    {
        var why = reason == null ? "" : $"<p>Powód: {Html(reason)}</p>";

        return new EmailContent(
            $"{SingleLine(studioName)} nie może przyjąć Twojej rezerwacji",
            $"""
             <p>Cześć {Html(customerName)},</p>
             <p>Niestety studio <strong>{Html(studioName)}</strong> nie może przyjąć Twojej prośby o rezerwację.</p>
             {why}
             <p>Możesz wybrać inny termin albo inne studio na mapie.</p>
             """);
    }

    // Escapes what matters in HTML (<, >, &, quotes) but keeps Polish letters readable, unlike WebUtility.HtmlEncode
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    private static string Html(string text) => Encoder.Encode(text);

    // A subject can't hold line breaks (MailMessage throws on them)
    private static string SingleLine(string text) =>
        string.Join(' ', text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
