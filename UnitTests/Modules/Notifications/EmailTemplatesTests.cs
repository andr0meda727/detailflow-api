using Notifications.Infrastructure.Email;
using Shared.Kernel.Time;

namespace Unit.Modules.Notifications;

[TestFixture]
public class EmailTemplatesTests
{
    private static readonly DateTime Expires = new(2026, 11, 13, 14, 30, 0, DateTimeKind.Utc);
    private const string AcceptUrl = "https://app.test/accept-invitation?token=abc&x=1";
    private static readonly TimeZoneInfo Warsaw = TimeZones.Find("Europe/Warsaw");

    [Test]
    public void Invitation_NamesTheStudioAndLinksToTheAcceptPage()
    {
        var email = EmailTemplates.Invitation("Detail Studio", AcceptUrl, Expires, Warsaw);

        Assert.That(email.Subject, Is.EqualTo("Zaproszenie do studia Detail Studio"));
        Assert.That(email.Html, Does.Contain("<strong>Detail Studio</strong>"));
        Assert.That(email.Html, Does.Contain("href=\"https://app.test/accept-invitation?token=abc&amp;x=1\""));
        // 14:30 UTC is 15:30 in Warsaw in November
        Assert.That(email.Html, Does.Contain("13.11.2026 15:30 (Europe/Warsaw)"));
    }

    [Test]
    public void Invitation_EncodesMarkupInTheStudioName()
    {
        // An owner controls the studio name and the recipient, so markup in it must not reach the email as HTML
        var email = EmailTemplates.Invitation("<a href=\"https://evil.test\">Kliknij</a>", AcceptUrl, Expires, Warsaw);

        Assert.That(email.Html, Does.Not.Contain("<a href=\"https://evil.test\""));
        Assert.That(email.Html, Does.Contain("&lt;a href=&quot;https://evil.test&quot;&gt;Kliknij&lt;/a&gt;"));
    }

    [Test]
    public void Invitation_SubjectStaysOnOneLine()
    {
        var email = EmailTemplates.Invitation("Detail\r\nBcc: someone@example.com", AcceptUrl, Expires, Warsaw);

        Assert.That(email.Subject, Does.Not.Contain("\r").And.Not.Contain("\n"));
        Assert.That(email.Subject, Is.EqualTo("Zaproszenie do studia Detail Bcc: someone@example.com"));
    }

    [Test]
    public void BookingsReport_ShowsThePeriod()
    {
        var email = EmailTemplates.BookingsReport(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));

        Assert.That(email.Subject, Is.EqualTo("Raport wizyt 01.10.2026–31.10.2026"));
        Assert.That(email.Html, Does.Contain("01.10.2026 — 31.10.2026"));
    }
}
