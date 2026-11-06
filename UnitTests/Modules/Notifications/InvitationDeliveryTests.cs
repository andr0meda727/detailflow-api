using Identity.Application.Abstractions;
using Identity.Contracts;
using Identity.Infrastructure.Consumers;
using MassTransit;
using Notifications.Infrastructure.Consumers;
using Notifications.Infrastructure.Email;
using NSubstitute;

namespace Unit.Modules.Notifications;

/// <summary>
/// Both consumers receive every EmployeeInvited. Between them, each invited person must get exactly one message:
/// a Supabase sign-up invite without a login, our own email with one.
/// </summary>
[TestFixture]
public class InvitationDeliveryTests
{
    private IAuthProvider _auth = null!;
    private IEmailSender _email = null!;

    [SetUp]
    public void SetUp()
    {
        _auth = Substitute.For<IAuthProvider>();
        _email = Substitute.For<IEmailSender>();
    }

    private static ConsumeContext<EmployeeInvited> Invited(bool hasAccount)
    {
        var context = Substitute.For<ConsumeContext<EmployeeInvited>>();
        context.Message.Returns(new EmployeeInvited(Guid.NewGuid(), Guid.NewGuid(), "anna@example.com", "Detail Studio",
            "https://app.test/accept-invitation?token=1", new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc),
            hasAccount));
        return context;
    }

    private async Task DeliverAsync(ConsumeContext<EmployeeInvited> context)
    {
        await new AccountInviteConsumer(_auth).Consume(context);
        await new InvitationEmailConsumer(_email).Consume(context);
    }

    [Test]
    public async Task PersonWithoutALogin_GetsOnlyTheSupabaseInvite()
    {
        await DeliverAsync(Invited(hasAccount: false));

        await _auth.Received(1).InviteUserAsync("anna@example.com", "https://app.test/accept-invitation?token=1",
            Arg.Any<CancellationToken>());
        await _email.DidNotReceiveWithAnyArgs().SendInvitationEmailAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task PersonWithALogin_GetsOnlyOurEmail()
    {
        await DeliverAsync(Invited(hasAccount: true));

        await _email.Received(1).SendInvitationEmailAsync("anna@example.com", "Detail Studio",
            "https://app.test/accept-invitation?token=1", new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc));
        await _auth.DidNotReceiveWithAnyArgs().InviteUserAsync(default!, default!, default);
    }
}
