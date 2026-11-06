using MassTransit;
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
    private static readonly DateTime From = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To = new(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc);

    // A PDF file starts with %PDF
    private static bool IsPdf(byte[] bytes) => bytes.AsSpan().StartsWith("%PDF"u8);

    [Test]
    public async Task ReadsTheRequestedStudioAndEmailsThePdfToTheRequester()
    {
        var studioId = Guid.NewGuid();
        var source = Substitute.For<IBookingReportSource>();
        source.GetBookingsAsync(studioId, From, To, Arg.Any<CancellationToken>())
            .Returns([new BookingReportRow(From.AddDays(3), "Jan", ["Polerowanie"], "Zakończona")]);
        var email = Substitute.For<IEmailSender>();
        var context = Substitute.For<ConsumeContext<GenerateBookingsReport>>();
        context.Message.Returns(new GenerateBookingsReport(studioId, "owner@example.com", From, To));

        await new BookingsReportConsumer(source, new BookingsReportPdf(), email).Consume(context);

        // The studio comes from the message: there is no tenant outside a request
        await source.Received(1).GetBookingsAsync(studioId, From, To, Arg.Any<CancellationToken>());
        await email.Received(1).SendReportEmailAsync("owner@example.com", From, To,
            Arg.Is<byte[]>(pdf => IsPdf(pdf)));
    }
}
