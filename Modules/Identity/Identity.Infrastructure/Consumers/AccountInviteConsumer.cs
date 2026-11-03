using Identity.Application.Abstractions;
using Identity.Contracts;
using MassTransit;

namespace Identity.Infrastructure.Consumers;

/// <summary>
/// Someone without a login was invited: Supabase creates the account and emails the sign-up link,
/// which leads to the invitation page. People who already have a login get a normal email instead
/// (handled by the email consumer).
/// </summary>
public class AccountInviteConsumer(IAuthProvider authProvider) : IConsumer<EmployeeInvited>
{
    public async Task Consume(ConsumeContext<EmployeeInvited> context)
    {
        var message = context.Message;
        if (message.HasAccount) return;

        await authProvider.InviteUserAsync(message.Email, message.AcceptUrl, context.CancellationToken);
    }
}
