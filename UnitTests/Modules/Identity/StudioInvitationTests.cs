using Identity.Domain.Invitations;
using Identity.Domain.Studios;

namespace Unit.Modules.Identity;

[TestFixture]
public class StudioInvitationTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    private static StudioInvitation NewInvitation() =>
        StudioInvitation.Create(" jan@test.pl ", Roles.Employee, Guid.NewGuid(), Now);

    [Test]
    public void Create_IsOpenForSevenDays()
    {
        var invitation = NewInvitation();

        Assert.That(invitation.Email, Is.EqualTo("jan@test.pl"));
        Assert.That(invitation.Status, Is.EqualTo(InvitationStatus.Pending));
        Assert.That(invitation.IsOpen(Now.AddDays(7).AddSeconds(-1)), Is.True);
        Assert.That(invitation.IsOpen(Now.AddDays(7)), Is.False);
    }

    [Test]
    public void Accept_ComparesEmailsCaseInsensitively()
    {
        var invitation = NewInvitation();

        Assert.That(invitation.Accept("JAN@test.PL", Now).Success, Is.True);
        Assert.That(invitation.Status, Is.EqualTo(InvitationStatus.Accepted));
    }

    [Test]
    public void Accept_ByAnotherAddress_IsForbidden()
    {
        var invitation = NewInvitation();

        Assert.That(invitation.Accept("ewa@test.pl", Now).Error, Is.EqualTo(InvitationErrors.WrongUser));
        Assert.That(invitation.Status, Is.EqualTo(InvitationStatus.Pending));
    }

    [Test]
    public void AnsweringTwice_IsRejected()
    {
        var invitation = NewInvitation();
        invitation.Decline("jan@test.pl", Now);

        Assert.That(invitation.Accept("jan@test.pl", Now).ErrorCode, Is.EqualTo("ALREADY_USED"));
    }

    [Test]
    public void AnsweringAfterExpiry_MarksItExpired()
    {
        var invitation = NewInvitation();

        Assert.That(invitation.Accept("jan@test.pl", Now.AddDays(8)).Error, Is.EqualTo(InvitationErrors.Expired));
        Assert.That(invitation.Status, Is.EqualTo(InvitationStatus.Expired));
    }

    [Test]
    public void Reissue_AfterDecline_GivesANewTokenAndPeriod()
    {
        var invitation = NewInvitation();
        var firstToken = invitation.Token;
        invitation.Decline("jan@test.pl", Now);

        invitation.Reissue(Roles.Manager, Guid.NewGuid(), Now.AddDays(1));

        Assert.That(invitation.Status, Is.EqualTo(InvitationStatus.Pending));
        Assert.That(invitation.Token, Is.Not.EqualTo(firstToken));
        Assert.That(invitation.RoleId, Is.EqualTo(Roles.Manager));
        Assert.That(invitation.ExpiresAt, Is.EqualTo(Now.AddDays(8)));
    }

    [Test]
    public void Reissue_WhileStillOpen_IsNotAllowed()
    {
        Assert.That(() => NewInvitation().Reissue(Roles.Employee, Guid.NewGuid(), Now), Throws.InvalidOperationException);
    }
}
