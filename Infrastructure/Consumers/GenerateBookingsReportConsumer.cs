using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Messages;
using MassTransit;

namespace Infrastructure.Consumers;

public class GenerateBookingsReportConsumer(
    IBookingRepository bookingRepository,
    IReportPdfService reportPdfService,
    IEmailService emailService) : IConsumer<GenerateBookingsReportCommand>
{
    public async Task Consume(ConsumeContext<GenerateBookingsReportCommand> context)
    {
        var bookings = await bookingRepository.GetByStudioAndDateRangeAsync(
            context.Message.StudioId, context.Message.DateFrom, context.Message.DateTo);

        var pdfBytes = reportPdfService.GenerateBookingsReport(
            bookings, context.Message.DateFrom, context.Message.DateTo);

        await emailService.SendReportEmailAsync(
            context.Message.RequestedByEmail,
            context.Message.DateFrom,
            context.Message.DateTo,
            pdfBytes);
    }
}