using System.Collections.Concurrent;
using Notifications.Infrastructure.Email;

namespace IntegrationTests.Api;

/// <summary>
/// Records the emails the app would send, in place of SMTP.
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    public ConcurrentBag<(string To, string StudioName, string AcceptUrl)> Invitations { get; } = [];

    public ConcurrentBag<(string To, DateOnly DateFrom, DateOnly DateTo, byte[] Pdf)> Reports { get; } = [];

    public Task SendInvitationEmailAsync(
        string to, string studioName, string acceptUrl, DateTime expiresAt, TimeZoneInfo timeZone)
    {
        Invitations.Add((to, studioName, acceptUrl));
        return Task.CompletedTask;
    }

    public Task SendReportEmailAsync(string to, DateOnly dateFrom, DateOnly dateTo, byte[] pdfBytes)
    {
        Reports.Add((to, dateFrom, dateTo, pdfBytes));
        return Task.CompletedTask;
    }
}
