using Core.Interfaces.Services;
using Identity.Contracts;
using MassTransit;

namespace Infrastructure.Consumers;

/// <summary>
/// Emails the accept link to an invited person who already has a login. People without one get
/// Supabase's sign-up email instead, sent by the Identity module.
/// </summary>
public class InvitationEmailConsumer(IEmailService emailService) : IConsumer<EmployeeInvited>
{
    public async Task Consume(ConsumeContext<EmployeeInvited> context)
    {
        var message = context.Message;
        if (!message.HasAccount) return;

        await emailService.SendInvitationEmailAsync(message.Email, message.StudioName, message.AcceptUrl, message.ExpiresAt);
    }
}
