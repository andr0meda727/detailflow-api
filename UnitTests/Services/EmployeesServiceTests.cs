using Core.Constants;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;
using Core.Services;
using Core.Settings;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using InviteEmployeeDto = Core.DTOs.Auth.InviteEmployeeDto;

namespace Unit.Services;

[TestFixture]
public class EmployeesServiceTests
{
    private IInvitationSender _invitationSender;
    private ITenantContext _tenantContext;
    private IUserRepository _userRepository;
    private IStudioMemberRepository _studioMemberRepository;
    private IStudioInvitationRepository _invitationRepository;
    private EmployeesService _sut;

    private readonly Guid _studioId = Guid.NewGuid();
    private readonly Guid _invitingUserId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _invitationSender = Substitute.For<IInvitationSender>();
        _tenantContext = Substitute.For<ITenantContext>();
        _userRepository = Substitute.For<IUserRepository>();
        _studioMemberRepository = Substitute.For<IStudioMemberRepository>();
        _invitationRepository = Substitute.For<IStudioInvitationRepository>();

        _tenantContext.StudioId.Returns(_studioId);
        _tenantContext.UserId.Returns(_invitingUserId);

        _sut = new EmployeesService(
            _invitationSender, _tenantContext,
            _userRepository, _studioMemberRepository, _invitationRepository,
            new FrontendSettings("https://app.example.com/"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Self-invite
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task InviteEmployeeAsync_ReturnsSelfInviteError_WhenEmailMatchesInviter()
    {
        var dto = new InviteEmployeeDto { Email = "owner@studio.pl", RoleId = Roles.Employee };
        var invitingUser = BuildUser(_invitingUserId, "owner@studio.pl");

        _userRepository.GetByIdAsync(_invitingUserId).Returns(invitingUser);

        var result = await _sut.InviteEmployeeAsync(dto);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("SELF_INVITE"));
    }

    [Test]
    public async Task InviteEmployeeAsync_EmailComparison_IsCaseSensitive_SoUppercaseDoesNotTriggerSelfInvite()
    {
        // The service uses == (case-sensitive), so "OWNER@STUDIO.PL" != "owner@studio.pl".
        // The self-invite guard will NOT fire, and the flow continues to a successful invite.
        var dto = new InviteEmployeeDto { Email = "OWNER@STUDIO.PL", RoleId = Roles.Employee };
        var invitingUser = BuildUser(_invitingUserId, "owner@studio.pl");
        var invitation = BuildInvitation(_studioId, "OWNER@STUDIO.PL");

        _userRepository.GetByIdAsync(_invitingUserId).Returns(invitingUser);
        _studioMemberRepository.GetByUserEmailAndStudioAsync("OWNER@STUDIO.PL", _studioId).ReturnsNull();
        _invitationRepository.GetPendingByEmailAndStudioAsync("OWNER@STUDIO.PL", _studioId).ReturnsNull();
        _invitationRepository.AddAsync(Arg.Any<StudioInvitation>()).Returns(invitation);
        _userRepository.GetUserByEmailAsync("OWNER@STUDIO.PL").ReturnsNull();

        var result = await _sut.InviteEmployeeAsync(dto);

        Assert.That(result.ErrorCode, Is.Not.EqualTo("SELF_INVITE"));
        Assert.That(result.Success, Is.True);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Already a member
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task InviteEmployeeAsync_ReturnsAlreadyMemberError_WhenUserIsAlreadyMember()
    {
        var email = "employee@studio.pl";
        var dto = new InviteEmployeeDto { Email = email, RoleId = Roles.Employee };

        _userRepository.GetByIdAsync(_invitingUserId).Returns(BuildUser(_invitingUserId, "owner@studio.pl"));
        _studioMemberRepository.GetByUserEmailAndStudioAsync(email, _studioId)
            .Returns(BuildStudioMember(Guid.NewGuid(), _studioId, Roles.Employee));

        var result = await _sut.InviteEmployeeAsync(dto);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("ALREADY_MEMBER"));
    }

    [Test]
    public async Task InviteEmployeeAsync_ReturnsAlreadyMemberError_WithRoleChangeHint_WhenDifferentRole()
    {
        var email = "employee@studio.pl";
        var dto = new InviteEmployeeDto { Email = email, RoleId = Roles.Manager }; // different role

        _userRepository.GetByIdAsync(_invitingUserId).Returns(BuildUser(_invitingUserId, "owner@studio.pl"));
        _studioMemberRepository.GetByUserEmailAndStudioAsync(email, _studioId)
            .Returns(BuildStudioMember(Guid.NewGuid(), _studioId, Roles.Employee)); // existing role

        var result = await _sut.InviteEmployeeAsync(dto);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("ALREADY_MEMBER"));
        Assert.That(result.ErrorMessage, Does.Contain("roli"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Pending invitation
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task InviteEmployeeAsync_ReturnsInvitationPendingError_WhenActiveInvitationExists()
    {
        var email = "new@studio.pl";
        var dto = new InviteEmployeeDto { Email = email, RoleId = Roles.Employee };

        _userRepository.GetByIdAsync(_invitingUserId).Returns(BuildUser(_invitingUserId, "owner@studio.pl"));
        _studioMemberRepository.GetByUserEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.GetPendingByEmailAndStudioAsync(email, _studioId)
            .Returns(BuildInvitation(_studioId, email));

        var result = await _sut.InviteEmployeeAsync(dto);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("INVITATION_PENDING"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Successful invite
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task InviteEmployeeAsync_ReturnsSuccess_WhenAllChecksPass()
    {
        var email = "newuser@studio.pl";
        var dto = new InviteEmployeeDto { Email = email, RoleId = Roles.Employee };
        var invitation = BuildInvitation(_studioId, email);

        _userRepository.GetByIdAsync(_invitingUserId).Returns(BuildUser(_invitingUserId, "owner@studio.pl"));
        _studioMemberRepository.GetByUserEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.GetPendingByEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.AddAsync(Arg.Any<StudioInvitation>()).Returns(invitation);
        _userRepository.GetUserByEmailAsync(email).ReturnsNull();

        var result = await _sut.InviteEmployeeAsync(dto);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(invitation.Id));
    }

    [Test]
    public async Task InviteEmployeeAsync_SavesInvitationWithCorrectFields()
    {
        var email = "newuser@studio.pl";
        var dto = new InviteEmployeeDto { Email = email, RoleId = Roles.Employee };
        var invitation = BuildInvitation(_studioId, email);

        _userRepository.GetByIdAsync(_invitingUserId).Returns(BuildUser(_invitingUserId, "owner@studio.pl"));
        _studioMemberRepository.GetByUserEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.GetPendingByEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.AddAsync(Arg.Any<StudioInvitation>()).Returns(invitation);
        _userRepository.GetUserByEmailAsync(email).ReturnsNull();

        await _sut.InviteEmployeeAsync(dto);

        await _invitationRepository.Received(1).AddAsync(Arg.Is<StudioInvitation>(i =>
            i.Email == email &&
            i.StudioId == _studioId &&
            i.RoleId == Roles.Employee &&
            i.InvitedBy == _invitingUserId &&
            i.Status == InvitationStatus.Pending));
    }

    [Test]
    public async Task InviteEmployeeAsync_SetsExpiryToSevenDays()
    {
        var email = "newuser@studio.pl";
        var dto = new InviteEmployeeDto { Email = email, RoleId = Roles.Employee };
        var invitation = BuildInvitation(_studioId, email);
        StudioInvitation? captured = null;

        _userRepository.GetByIdAsync(_invitingUserId).Returns(BuildUser(_invitingUserId, "owner@studio.pl"));
        _studioMemberRepository.GetByUserEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.GetPendingByEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.AddAsync(Arg.Do<StudioInvitation>(i => captured = i)).Returns(invitation);
        _userRepository.GetUserByEmailAsync(email).ReturnsNull();

        await _sut.InviteEmployeeAsync(dto);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ExpiresAt, Is.EqualTo(DateTime.UtcNow.AddDays(7)).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public async Task InviteEmployeeAsync_CallsInvitationSender()
    {
        var email = "newuser@studio.pl";
        var dto = new InviteEmployeeDto { Email = email, RoleId = Roles.Employee };
        var invitation = BuildInvitation(_studioId, email);

        _userRepository.GetByIdAsync(_invitingUserId).Returns(BuildUser(_invitingUserId, "owner@studio.pl"));
        _studioMemberRepository.GetByUserEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.GetPendingByEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.AddAsync(Arg.Any<StudioInvitation>()).Returns(invitation);
        _userRepository.GetUserByEmailAsync(email).ReturnsNull();

        await _sut.InviteEmployeeAsync(dto);

        await _invitationSender.Received(1).SendInvitationAsync(
            invitation, false, _studioId, Arg.Any<string>());
    }

    [Test]
    public async Task InviteEmployeeAsync_SendsAcceptLinkToConfiguredFrontend()
    {
        var email = "newuser@studio.pl";
        var dto = new InviteEmployeeDto { Email = email, RoleId = Roles.Employee };
        var invitation = BuildInvitation(_studioId, email);

        _userRepository.GetByIdAsync(_invitingUserId).Returns(BuildUser(_invitingUserId, "owner@studio.pl"));
        _studioMemberRepository.GetByUserEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.GetPendingByEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.AddAsync(Arg.Any<StudioInvitation>()).Returns(invitation);

        await _sut.InviteEmployeeAsync(dto);

        await _invitationSender.Received(1).SendInvitationAsync(
            invitation, Arg.Any<bool>(), _studioId,
            $"https://app.example.com/accept-invitation?token={invitation.Token}");
    }

    [Test]
    public async Task InviteEmployeeAsync_SendsWithUserExistsTrue_WhenUserExistsInSystem()
    {
        var email = "existing@studio.pl";
        var dto = new InviteEmployeeDto { Email = email, RoleId = Roles.Employee };
        var invitation = BuildInvitation(_studioId, email);

        _userRepository.GetByIdAsync(_invitingUserId).Returns(BuildUser(_invitingUserId, "owner@studio.pl"));
        _studioMemberRepository.GetByUserEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.GetPendingByEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.AddAsync(Arg.Any<StudioInvitation>()).Returns(invitation);
        _userRepository.GetUserByEmailAsync(email).Returns(BuildUser(Guid.NewGuid(), email));

        await _sut.InviteEmployeeAsync(dto);

        await _invitationSender.Received(1).SendInvitationAsync(
            invitation, true, _studioId, Arg.Any<string>());
    }

    [Test]
    public async Task InviteEmployeeAsync_ThrowsInvalidOperation_WhenStudioIdIsNull()
    {
        _tenantContext.StudioId.Returns((Guid?)null);
        var dto = new InviteEmployeeDto { Email = "test@test.pl", RoleId = Roles.Employee };

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.InviteEmployeeAsync(dto));
    }

    [Test]
    public async Task InviteEmployeeAsync_ReturnsSuccess_WhenUserWasPreviouslyRemovedFromStudio()
    {
        var email = "employee@studio.pl";
        var dto = new InviteEmployeeDto { Email = email, RoleId = Roles.Employee };
        var invitation = BuildInvitation(_studioId, email);

        _userRepository.GetByIdAsync(_invitingUserId).Returns(BuildUser(_invitingUserId, "owner@studio.pl"));
        _studioMemberRepository.GetByUserEmailAndStudioAsync(email, _studioId)
            .Returns(BuildStudioMember(Guid.NewGuid(), _studioId, Roles.Employee, isActive: false));
        _invitationRepository.GetPendingByEmailAndStudioAsync(email, _studioId).ReturnsNull();
        _invitationRepository.AddAsync(Arg.Any<StudioInvitation>()).Returns(invitation);
        _userRepository.GetUserByEmailAsync(email).Returns(BuildUser(Guid.NewGuid(), email));

        var result = await _sut.InviteEmployeeAsync(dto);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.Null);
        await _invitationRepository.Received(1).AddAsync(Arg.Any<StudioInvitation>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static User BuildUser(Guid id, string email) => new()
    {
        Id = id,
        Email = email,
        IsActive = true
    };

    private static StudioMember BuildStudioMember(Guid userId, Guid studioId, long roleId, bool isActive = true) => new()
    {
        UserId = userId,
        StudioId = studioId,
        RoleId = roleId,
        IsActive = isActive
    };

    private static StudioInvitation BuildInvitation(Guid studioId, string email) => new()
    {
        Id = Guid.NewGuid(),
        StudioId = studioId,
        Email = email,
        RoleId = Roles.Employee,
        Token = Guid.NewGuid(),
        Status = InvitationStatus.Pending,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };
}