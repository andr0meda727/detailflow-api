using Core.Constants;
using Core.DTOs.Employees;
using Core.Interfaces.Repositories;
using Core.Models;
using Core.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Unit.Services;

[TestFixture]
public class InvitationServiceTests
{
    private IStudioInvitationRepository _invitationRepository;
    private IStudioMemberRepository _studioMemberRepository;
    private IUserRepository _userRepository;
    private InvitationService _sut;

    [SetUp]
    public void SetUp()
    {
        _invitationRepository = Substitute.For<IStudioInvitationRepository>();
        _studioMemberRepository = Substitute.For<IStudioMemberRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new InvitationService(_invitationRepository, _studioMemberRepository, _userRepository);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetByTokenAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetByTokenAsync_ReturnsNull_WhenInvitationNotFound()
    {
        _invitationRepository.GetByTokenAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.GetByTokenAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByTokenAsync_ReturnsNull_WhenInvitationIsNotPending()
    {
        var token = Guid.NewGuid();
        var invitation = BuildInvitation(token, status: InvitationStatus.Accepted);
        _invitationRepository.GetByTokenAsync(token).Returns(invitation);

        var result = await _sut.GetByTokenAsync(token);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByTokenAsync_ReturnsNull_AndMarksExpired_WhenExpired()
    {
        var token = Guid.NewGuid();
        var invitation = BuildInvitation(token, expiresAt: DateTime.UtcNow.AddDays(-1));
        _invitationRepository.GetByTokenAsync(token).Returns(invitation);

        var result = await _sut.GetByTokenAsync(token);

        Assert.That(result, Is.Null);
        Assert.That(invitation.Status, Is.EqualTo(InvitationStatus.Expired));
        await _invitationRepository.Received(1).UpdateAsync(invitation);
    }

    [Test]
    public async Task GetByTokenAsync_ReturnsDto_WhenValid()
    {
        var token = Guid.NewGuid();
        var invitation = BuildInvitation(token);
        _invitationRepository.GetByTokenAsync(token).Returns(invitation);

        var result = await _sut.GetByTokenAsync(token);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<InvitationResponseDto>());
    }

    [Test]
    public async Task GetByTokenAsync_ReturnsDtoWithCorrectData()
    {
        var token = Guid.NewGuid();
        var invitation = BuildInvitation(token);
        _invitationRepository.GetByTokenAsync(token).Returns(invitation);

        var result = await _sut.GetByTokenAsync(token);

        Assert.That(result!.Email, Is.EqualTo(invitation.Email));
        Assert.That(result.StudioName, Is.EqualTo(invitation.Studio.Name));
        Assert.That(result.Role, Is.EqualTo(invitation.Role.RoleName));
        Assert.That(result.ExpiresAt, Is.EqualTo(invitation.ExpiresAt));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AcceptAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task AcceptAsync_ReturnsFail_WhenInvitationNotFound()
    {
        _invitationRepository.GetByTokenAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.AcceptAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("NOT_FOUND"));
    }

    [Test]
    public async Task AcceptAsync_ReturnsFail_AndMarksExpired_WhenExpired()
    {
        var token = Guid.NewGuid();
        var invitation = BuildInvitation(token, expiresAt: DateTime.UtcNow.AddDays(-1));
        _invitationRepository.GetByTokenAsync(token).Returns(invitation);

        var result = await _sut.AcceptAsync(token, Guid.NewGuid());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("EXPIRED"));
        Assert.That(invitation.Status, Is.EqualTo(InvitationStatus.Expired));
    }

    [Test]
    public async Task AcceptAsync_ReturnsFail_WhenAlreadyAccepted()
    {
        var token = Guid.NewGuid();
        var invitation = BuildInvitation(token, status: InvitationStatus.Accepted);
        _invitationRepository.GetByTokenAsync(token).Returns(invitation);

        var result = await _sut.AcceptAsync(token, Guid.NewGuid());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("ALREADY_USED"));
    }

    [Test]
    public async Task AcceptAsync_ReturnsFail_WhenAlreadyDeclined()
    {
        var token = Guid.NewGuid();
        var invitation = BuildInvitation(token, status: InvitationStatus.Declined);
        _invitationRepository.GetByTokenAsync(token).Returns(invitation);

        var result = await _sut.AcceptAsync(token, Guid.NewGuid());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("ALREADY_USED"));
    }

    [Test]
    public async Task AcceptAsync_ReturnsFail_WhenEmailMismatch()
    {
        var token = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitation = BuildInvitation(token, email: "invited@test.pl");
        var user = BuildUser(userId, "different@test.pl");

        _invitationRepository.GetByTokenAsync(token).Returns(invitation);
        _userRepository.GetByIdAsync(userId).Returns(user);

        var result = await _sut.AcceptAsync(token, userId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("WRONG_USER"));
    }

    [Test]
    public async Task AcceptAsync_EmailComparison_IsCaseInsensitive()
    {
        var token = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitation = BuildInvitation(token, email: "INVITED@TEST.PL");
        var user = BuildUser(userId, "invited@test.pl");

        _invitationRepository.GetByTokenAsync(token).Returns(invitation);
        _userRepository.GetByIdAsync(userId).Returns(user);
        _studioMemberRepository.GetByUserIdAndStudioAsync(userId, invitation.StudioId).ReturnsNull();
        _studioMemberRepository.AddAsync(Arg.Any<StudioMember>()).Returns(new StudioMember());

        var result = await _sut.AcceptAsync(token, userId);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task AcceptAsync_ReturnsOk_WhenAlreadyMember()
    {
        var token = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitation = BuildInvitation(token, email: "user@test.pl");
        var user = BuildUser(userId, "user@test.pl");
        var existingMember = new StudioMember { UserId = userId, StudioId = invitation.StudioId };

        _invitationRepository.GetByTokenAsync(token).Returns(invitation);
        _userRepository.GetByIdAsync(userId).Returns(user);
        _studioMemberRepository.GetByUserIdAndStudioAsync(userId, invitation.StudioId).Returns(existingMember);

        var result = await _sut.AcceptAsync(token, userId);

        Assert.That(result.Success, Is.True);
        await _studioMemberRepository.DidNotReceive().AddAsync(Arg.Any<StudioMember>());
    }

    [Test]
    public async Task AcceptAsync_AddsStudioMember_WhenNotAlreadyMember()
    {
        var token = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitation = BuildInvitation(token, email: "user@test.pl");
        var user = BuildUser(userId, "user@test.pl");

        _invitationRepository.GetByTokenAsync(token).Returns(invitation);
        _userRepository.GetByIdAsync(userId).Returns(user);
        _studioMemberRepository.GetByUserIdAndStudioAsync(userId, invitation.StudioId).ReturnsNull();
        _studioMemberRepository.AddAsync(Arg.Any<StudioMember>()).Returns(new StudioMember());

        await _sut.AcceptAsync(token, userId);

        await _studioMemberRepository.Received(1).AddAsync(Arg.Is<StudioMember>(m =>
            m.UserId == userId &&
            m.StudioId == invitation.StudioId &&
            m.RoleId == invitation.RoleId));
    }

    [Test]
    public async Task AcceptAsync_MarksInvitationAsAccepted()
    {
        var token = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitation = BuildInvitation(token, email: "user@test.pl");
        var user = BuildUser(userId, "user@test.pl");

        _invitationRepository.GetByTokenAsync(token).Returns(invitation);
        _userRepository.GetByIdAsync(userId).Returns(user);
        _studioMemberRepository.GetByUserIdAndStudioAsync(userId, invitation.StudioId).ReturnsNull();
        _studioMemberRepository.AddAsync(Arg.Any<StudioMember>()).Returns(new StudioMember());

        await _sut.AcceptAsync(token, userId);

        Assert.That(invitation.Status, Is.EqualTo(InvitationStatus.Accepted));
        await _invitationRepository.Received(1).UpdateAsync(invitation);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DeclineAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeclineAsync_ReturnsFail_WhenInvitationNotFound()
    {
        _invitationRepository.GetByTokenAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.DeclineAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("NOT_FOUND"));
    }

    [Test]
    public async Task DeclineAsync_ReturnsFail_WhenAlreadyUsed()
    {
        var token = Guid.NewGuid();
        var invitation = BuildInvitation(token, status: InvitationStatus.Accepted);
        _invitationRepository.GetByTokenAsync(token).Returns(invitation);

        var result = await _sut.DeclineAsync(token, Guid.NewGuid());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("ALREADY_USED"));
    }

    [Test]
    public async Task DeclineAsync_ReturnsFail_WhenWrongUser()
    {
        var token = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitation = BuildInvitation(token, email: "invited@test.pl");
        var user = BuildUser(userId, "wrong@test.pl");

        _invitationRepository.GetByTokenAsync(token).Returns(invitation);
        _userRepository.GetByIdAsync(userId).Returns(user);

        var result = await _sut.DeclineAsync(token, userId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("WRONG_USER"));
    }

    [Test]
    public async Task DeclineAsync_ReturnsOk_AndMarksDeclined()
    {
        var token = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitation = BuildInvitation(token, email: "user@test.pl");
        var user = BuildUser(userId, "user@test.pl");

        _invitationRepository.GetByTokenAsync(token).Returns(invitation);
        _userRepository.GetByIdAsync(userId).Returns(user);

        var result = await _sut.DeclineAsync(token, userId);

        Assert.That(result.Success, Is.True);
        Assert.That(invitation.Status, Is.EqualTo(InvitationStatus.Declined));
        await _invitationRepository.Received(1).UpdateAsync(invitation);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static StudioInvitation BuildInvitation(
        Guid token,
        string email = "user@test.pl",
        string status = InvitationStatus.Pending,
        DateTime? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Token = token,
        Email = email,
        StudioId = Guid.NewGuid(),
        RoleId = Roles.Employee,
        Status = status,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
        Studio = new Studio { Id = Guid.NewGuid(), Name = "Test Studio" },
        Role = new Role { Id = Roles.Employee, RoleName = "Employee" },
        InvitedByUser = new User { Id = Guid.NewGuid(), Email = "owner@studio.pl" }
    };

    private static User BuildUser(Guid id, string email) => new()
    {
        Id = id,
        Email = email,
        IsActive = true
    };
}
