using MassTransit;
using Microsoft.Extensions.Time.Testing;
using Notifications.Contracts;
using Notifications.Infrastructure.Consumers;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Reports;
using NSubstitute;
using Scheduling.Contracts;

namespace Unit.Modules.Notifications;

[TestFixture]
public class BookingsReportConsumerTests
{
    private static readonly DateOnly From = new(2026, 10, 1);
    private static readonly DateOnly To = new(2026, 10, 31);

    // A PDF file starts with %PDF
    private static bool IsPdf(byte[] bytes) => bytes.AsSpan().StartsWith("%PDF"u8);

    private static async Task<IBookingReportSource> RunAsync(Guid studioId, string timeZone, IEmailSender email)
    {
        var source = Substitute.For<IBookingReportSource>();
        source.GetBookingsAsync(default, default, default, default)
            .ReturnsForAnyArgs([new BookingReportRow(new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc), "Jan", ["Polerowanie"], "Zakończona")]);
        var context = Substitute.For<ConsumeContext<GenerateBookingsReport>>();
        context.Message.Returns(new GenerateBookingsReport(studioId, "owner@example.com", From, To, timeZone));

        await new BookingsReportConsumer(source, new BookingsReportPdf(), email, new FakeTimeProvider())
            .Consume(context);

        return source;
    }

    [Test]
    public async Task ReadsTheStudiosLocalDaysAndEmailsThePdfToTheRequester()
    {
        var studioId = Guid.NewGuid();
        var email = Substitute.For<IEmailSender>();

        var source = await RunAsync(studioId, "Europe/Warsaw", email);

        // October in Warsaw: from midnight on the 1st (summer time, UTC+2) to midnight on 1 November (winter time, UTC+1).
        // The studio comes from the message: there is no tenant outside a request.
        await source.Received(1).GetBookingsAsync(studioId,
            new DateTime(2026, 9, 30, 22, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 23, 0, 0, DateTimeKind.Utc),
            Arg.Any<CancellationToken>());
        await email.Received(1).SendReportEmailAsync("owner@example.com", From, To, Arg.Is<byte[]>(pdf => IsPdf(pdf)));
    }

    [Test]
    public async Task AnotherZone_ShiftsTheRange()
    {
        var studioId = Guid.NewGuid();

        var source = await RunAsync(studioId, "America/New_York", Substitute.For<IEmailSender>());

        // October in New York starts at 04:00 UTC (UTC-4)
        await source.Received(1).GetBookingsAsync(studioId,
            new DateTime(2026, 10, 1, 4, 0, 0, DateTimeKind.Utc), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
