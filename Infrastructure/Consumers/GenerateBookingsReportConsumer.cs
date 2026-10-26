using Core.Interfaces.Services;
using Core.Messages;
using MassTransit;
using Scheduling.Contracts;

namespace Infrastructure.Consumers;

public class GenerateBookingsReportConsumer(
    IBookingReportSource bookingReportSource,
    IReportPdfService reportPdfService,
    IEmailService emailService) : IConsumer<GenerateBookingsReportCommand>
{
    public async Task Consume(ConsumeContext<GenerateBookingsReportCommand> context)
    {
        var bookings = await bookingReportSource.GetBookingsAsync(
            context.Message.StudioId, context.Message.DateFrom, context.Message.DateTo, context.CancellationToken);

        var pdfBytes = reportPdfService.GenerateBookingsReport(
            bookings, context.Message.DateFrom, context.Message.DateTo);

        await emailService.SendReportEmailAsync(
            context.Message.RequestedByEmail,
            context.Message.DateFrom,
            context.Message.DateTo,
            pdfBytes);
    }
}
