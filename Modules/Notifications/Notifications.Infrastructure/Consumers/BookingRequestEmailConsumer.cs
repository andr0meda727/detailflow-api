using Identity.Contracts;
using MassTransit;
using Notifications.Infrastructure.Email;
using Scheduling.Contracts;
using Shared.Kernel.Time;

namespace Notifications.Infrastructure.Consumers;

/// <summary>
/// Emails about online booking requests: the studio's owners hear about a new request, and the customer hears how
/// the studio answered. Times are shown in the studio's time zone. Delivery is at least once, so a rare duplicate
/// email is possible; nothing else is changed here.
/// </summary>
public class BookingRequestEmailConsumer(IEmailSender emailSender, IStudioDirectory studios)
    : IConsumer<BookingRequestSubmitted>, IConsumer<BookingRequestDecided>
{
    public async Task Consume(ConsumeContext<BookingRequestSubmitted> context)
    {
        var message = context.Message;
        var zone = TimeZones.Find(await studios.FindTimeZoneAsync(message.StudioId, context.CancellationToken));
        var content = EmailTemplates.BookingRequestReceived(
            message.CustomerName, message.CustomerPhone, message.VehicleLicensePlate, message.Services,
            message.PreferredAt, zone);

        foreach (var owner in await studios.GetOwnerEmailsAsync(message.StudioId, context.CancellationToken))
        {
            await emailSender.SendAsync(owner, content);
        }
    }

    public async Task Consume(ConsumeContext<BookingRequestDecided> context)
    {
        var message = context.Message;

        EmailContent content;
        if (message.Accepted && message.ScheduledAt is { } scheduledAt)
        {
            var zone = TimeZones.Find(await studios.FindTimeZoneAsync(message.StudioId, context.CancellationToken));
            content = EmailTemplates.BookingRequestAccepted(message.CustomerName, message.StudioName, scheduledAt, zone);
        }
        else
        {
            content = EmailTemplates.BookingRequestRejected(message.CustomerName, message.StudioName, message.RejectionReason);
        }

        await emailSender.SendAsync(message.CustomerEmail, content);
    }
}
