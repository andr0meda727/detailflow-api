using MassTransit;
using Notifications.Contracts;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Reports;
using Scheduling.Contracts;
using Shared.Kernel.Time;

namespace Notifications.Infrastructure.Consumers;

public class BookingsReportConsumer(
    IBookingReportSource bookingReportSource,
    BookingsReportPdf reportPdf,
    IEmailSender emailSender,
    TimeProvider clock) : IConsumer<GenerateBookingsReport>
{
    public async Task Consume(ConsumeContext<GenerateBookingsReport> context)
    {
        var message = context.Message;
        var zone = TimeZones.Find(message.TimeZone);

        // The requested days are the studio's local days, so their UTC boundaries depend on its zone
        var (fromUtc, toUtcExclusive) = TimeZones.DaysToUtc(message.DateFrom, message.DateTo, zone);

        // Bookings come from the Scheduling module's contract; the studio is in the message, there is no tenant here
        var bookings = await bookingReportSource.GetBookingsAsync(
            message.StudioId, fromUtc, toUtcExclusive, context.CancellationToken);

        var pdf = reportPdf.Render(bookings, message.DateFrom, message.DateTo, zone, clock.GetUtcNow().UtcDateTime);

        await emailSender.SendReportEmailAsync(message.RequestedByEmail, message.DateFrom, message.DateTo, pdf);
    }
}
