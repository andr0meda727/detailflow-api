using Identity.Contracts;
using MassTransit;
using Notifications.Infrastructure.Email;

namespace Notifications.Infrastructure.Consumers;

/// <summary>
/// Emails the accept link to an invited person who already has a login. People without one get
/// Supabase's sign-up email instead, sent by the Identity module.
/// </summary>
public class InvitationEmailConsumer(SmtpEmailSender emailSender) : IConsumer<EmployeeInvited>
{
    public async Task Consume(ConsumeContext<EmployeeInvited> context)
    {
        var message = context.Message;
        if (!message.HasAccount) return;

        await emailSender.SendInvitationEmailAsync(message.Email, message.StudioName, message.AcceptUrl, message.ExpiresAt);
    }
}
