using Shared.Kernel.Tenancy;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;
using Core.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Unit.Services;

// ════════════════════════════════════════════════════════════════════════════
// InvitationSender
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class InvitationSenderTests
{
    private IAuthService _authService;
    private IEmailService _emailService;
    private IStudioRepository _studioRepository;
    private InvitationSender _sut;

    [SetUp]
    public void SetUp()
    {
        _authService = Substitute.For<IAuthService>();
        _emailService = Substitute.For<IEmailService>();
        _studioRepository = Substitute.For<IStudioRepository>();
        _sut = new InvitationSender(_authService, _emailService, _studioRepository);
    }

    [Test]
    public async Task SendInvitationAsync_InvitesViaAuth_WhenUserDoesNotExist()
    {
        var invitation = BuildInvitation("newuser@test.pl");
        var studioId = Guid.NewGuid();

        await _sut.SendInvitationAsync(invitation, userExists: false, studioId, "http://accept.url");

        await _authService.Received(1).InviteUserAsync("newuser@test.pl", "http://accept.url");
        await _emailService.DidNotReceive().SendInvitationEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>());
    }

    [Test]
    public async Task SendInvitationAsync_SendsEmail_WhenUserExists()
    {
        var studioId = Guid.NewGuid();
        var invitation = BuildInvitation("existing@test.pl");
        var studio = new Studio { Id = studioId, Name = "Detail Kings" };

        _studioRepository.GetByIdAsync(studioId).Returns(studio);

        await _sut.SendInvitationAsync(invitation, userExists: true, studioId, "http://accept.url");

        await _emailService.Received(1).SendInvitationEmailAsync(
            "existing@test.pl", "Detail Kings", "http://accept.url", invitation.ExpiresAt);
        await _authService.DidNotReceive().InviteUserAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task SendInvitationAsync_UsesDefaultStudioName_WhenStudioNotFound()
    {
        var studioId = Guid.NewGuid();
        var invitation = BuildInvitation("existing@test.pl");

        _studioRepository.GetByIdAsync(studioId).ReturnsNull();

        await _sut.SendInvitationAsync(invitation, userExists: true, studioId, "http://accept.url");

        await _emailService.Received(1).SendInvitationEmailAsync(
            Arg.Any<string>(), "DetailFlow Studio", Arg.Any<string>(), Arg.Any<DateTime>());
    }

    [Test]
    public async Task SendInvitationAsync_PassesCorrectAcceptUrl_ToAuthService()
    {
        var invitation = BuildInvitation("newuser@test.pl");
        const string url = "http://localhost:3000/accept?token=xyz";

        await _sut.SendInvitationAsync(invitation, userExists: false, Guid.NewGuid(), url);

        await _authService.Received(1).InviteUserAsync(Arg.Any<string>(), url);
    }

    private static StudioInvitation BuildInvitation(string email) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        StudioId = Guid.NewGuid(),
        RoleId = Core.Constants.Roles.Employee,
        Token = Guid.NewGuid(),
        Status = Core.Constants.InvitationStatus.Pending,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };
}
