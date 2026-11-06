using MassTransit;
using Notifications.Contracts;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Reports;
using Scheduling.Contracts;

namespace Notifications.Infrastructure.Consumers;

public class BookingsReportConsumer(
    IBookingReportSource bookingReportSource,
    BookingsReportPdf reportPdf,
    IEmailSender emailSender) : IConsumer<GenerateBookingsReport>
{
    public async Task Consume(ConsumeContext<GenerateBookingsReport> context)
    {
        var message = context.Message;

        // Bookings come from the Scheduling module's contract; the studio is in the message, there is no tenant here
        var bookings = await bookingReportSource.GetBookingsAsync(
            message.StudioId, message.DateFrom, message.DateTo, context.CancellationToken);

        var pdf = reportPdf.Render(bookings, message.DateFrom, message.DateTo);

        await emailSender.SendReportEmailAsync(message.RequestedByEmail, message.DateFrom, message.DateTo, pdf);
    }
}
