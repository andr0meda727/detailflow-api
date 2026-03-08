using Core.Constants;
using Core.DTOs.Auth;
using Core.DTOs.Studio;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;
using Core.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Unit.Services;

// ════════════════════════════════════════════════════════════════════════════
// OwnerRegistrationService
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class OwnerRegistrationServiceTests
{
    private IAuthService _authService;
    private IStudioRepository _studioRepository;
    private IStudioMemberRepository _studioMemberRepository;
    private OwnerRegistrationService _sut;

    [SetUp]
    public void SetUp()
    {
        _authService = Substitute.For<IAuthService>();
        _studioRepository = Substitute.For<IStudioRepository>();
        _studioMemberRepository = Substitute.For<IStudioMemberRepository>();
        _sut = new OwnerRegistrationService(_authService, _studioRepository, _studioMemberRepository);
    }

    [Test]
    public async Task RegisterAsync_ReturnsFail_WhenEmailAlreadyTaken()
    {
        var dto = BuildRegisterDto("existing@test.pl");
        _authService.UserExistsAsync("existing@test.pl").Returns(true);

        var result = await _sut.RegisterAsync(dto);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("EMAIL_TAKEN"));
    }

    [Test]
    public async Task RegisterAsync_DoesNotCreateStudio_WhenEmailTaken()
    {
        var dto = BuildRegisterDto("existing@test.pl");
        _authService.UserExistsAsync("existing@test.pl").Returns(true);

        await _sut.RegisterAsync(dto);

        await _studioRepository.DidNotReceive().AddAsync(Arg.Any<Studio>());
    }

    [Test]
    public async Task RegisterAsync_ReturnsSuccess_WhenAllDataValid()
    {
        var userId = Guid.NewGuid();
        var dto = BuildRegisterDto("newowner@test.pl");
        var studio = new Studio { Id = Guid.NewGuid(), Name = dto.Studio.Name };

        _authService.UserExistsAsync("newowner@test.pl").Returns(false);
        _authService.SignUpAsync(dto.Email, dto.Password, dto.FirstName, dto.LastName).Returns(userId);
        _studioRepository.AddAsync(Arg.Any<Studio>()).Returns(studio);
        _studioMemberRepository.AddAsync(Arg.Any<StudioMember>()).Returns(new StudioMember());

        var result = await _sut.RegisterAsync(dto);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.Null);
    }

    [Test]
    public async Task RegisterAsync_CreatesStudioWithCorrectName()
    {
        var userId = Guid.NewGuid();
        var dto = BuildRegisterDto(studioName: "Detail Kings");
        Studio? capturedStudio = null;

        _authService.UserExistsAsync(Arg.Any<string>()).Returns(false);
        _authService.SignUpAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>()).Returns(userId);
        _studioRepository.AddAsync(Arg.Do<Studio>(s => capturedStudio = s))
                          .Returns(ci => ci.Arg<Studio>());
        _studioMemberRepository.AddAsync(Arg.Any<StudioMember>()).Returns(new StudioMember());

        await _sut.RegisterAsync(dto);

        Assert.That(capturedStudio, Is.Not.Null);
        Assert.That(capturedStudio!.Name, Is.EqualTo("Detail Kings"));
    }

    [Test]
    public async Task RegisterAsync_CreatesOwnerMembership()
    {
        var userId = Guid.NewGuid();
        var studio = new Studio { Id = Guid.NewGuid(), Name = "Test Studio" };
        var dto = BuildRegisterDto();
        StudioMember? capturedMember = null;

        _authService.UserExistsAsync(Arg.Any<string>()).Returns(false);
        _authService.SignUpAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>()).Returns(userId);
        _studioRepository.AddAsync(Arg.Any<Studio>()).Returns(studio);
        _studioMemberRepository.AddAsync(Arg.Do<StudioMember>(m => capturedMember = m))
                                .Returns(new StudioMember());

        await _sut.RegisterAsync(dto);

        Assert.That(capturedMember, Is.Not.Null);
        Assert.That(capturedMember!.UserId, Is.EqualTo(userId));
        Assert.That(capturedMember.StudioId, Is.EqualTo(studio.Id));
        Assert.That(capturedMember.RoleId, Is.EqualTo(Roles.Owner));
    }

    [Test]
    public async Task RegisterAsync_CallsSignUpWithCorrectParams()
    {
        var dto = BuildRegisterDto("user@test.pl", "TestPass1!", "Jan", "Kowalski");
        _authService.UserExistsAsync("user@test.pl").Returns(false);
        _authService.SignUpAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>())
                    .Returns(Guid.NewGuid());
        _studioRepository.AddAsync(Arg.Any<Studio>()).Returns(new Studio { Id = Guid.NewGuid(), Name = "Test" });
        _studioMemberRepository.AddAsync(Arg.Any<StudioMember>()).Returns(new StudioMember());

        await _sut.RegisterAsync(dto);

        await _authService.Received(1).SignUpAsync("user@test.pl", "TestPass1!", "Jan", "Kowalski");
    }

    [Test]
    public async Task RegisterAsync_StudioHasNonEmptyId()
    {
        var userId = Guid.NewGuid();
        var dto = BuildRegisterDto();
        Studio? capturedStudio = null;

        _authService.UserExistsAsync(Arg.Any<string>()).Returns(false);
        _authService.SignUpAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>()).Returns(userId);
        _studioRepository.AddAsync(Arg.Do<Studio>(s => capturedStudio = s))
                          .Returns(ci => ci.Arg<Studio>());
        _studioMemberRepository.AddAsync(Arg.Any<StudioMember>()).Returns(new StudioMember());

        await _sut.RegisterAsync(dto);

        Assert.That(capturedStudio!.Id, Is.Not.EqualTo(Guid.Empty));
    }

    private static RegisterOwnerDto BuildRegisterDto(
        string email = "owner@test.pl",
        string password = "TestPass1!",
        string firstName = "Jan",
        string lastName = "Kowalski",
        string studioName = "Test Studio") => new()
    {
        Email = email,
        Password = password,
        FirstName = firstName,
        LastName = lastName,
        Studio = new CreateStudioDto { Name = studioName }
    };
}

// ════════════════════════════════════════════════════════════════════════════
// StudioService
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class StudioServiceTests
{
    private IStudioMemberRepository _studioMemberRepository;
    private StudioService _sut;

    [SetUp]
    public void SetUp()
    {
        _studioMemberRepository = Substitute.For<IStudioMemberRepository>();
        _sut = new StudioService(_studioMemberRepository);
    }

    [Test]
    public async Task GetStudiosForUserAsync_ReturnsAllStudios_ForGivenUser()
    {
        var userId = Guid.NewGuid();
        var members = new List<StudioMember>
        {
            BuildMember(userId, "Studio A", Roles.Owner),
            BuildMember(userId, "Studio B", Roles.Employee)
        };
        _studioMemberRepository.GetByUserIdWithStudioAsync(userId).Returns(members);

        var result = await _sut.GetStudiosForUserAsync(userId);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetStudiosForUserAsync_ReturnsEmptyList_WhenUserHasNoStudios()
    {
        var userId = Guid.NewGuid();
        _studioMemberRepository.GetByUserIdWithStudioAsync(userId).Returns(new List<StudioMember>());

        var result = await _sut.GetStudiosForUserAsync(userId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetStudiosForUserAsync_MapsStudioIdAndNameCorrectly()
    {
        var userId = Guid.NewGuid();
        var studioId = Guid.NewGuid();
        var member = BuildMember(userId, "My Detail Studio", Roles.Owner, studioId: studioId);
        member.Studio.Address = "ul. Testowa 1";

        _studioMemberRepository.GetByUserIdWithStudioAsync(userId).Returns(new List<StudioMember> { member });

        var result = await _sut.GetStudiosForUserAsync(userId);

        Assert.That(result[0].StudioId, Is.EqualTo(studioId));
        Assert.That(result[0].Name, Is.EqualTo("My Detail Studio"));
        Assert.That(result[0].Address, Is.EqualTo("ul. Testowa 1"));
    }

    [Test]
    public async Task GetStudiosForUserAsync_MapsRoleNameCorrectly()
    {
        var userId = Guid.NewGuid();
        var member = BuildMember(userId, "Studio", Roles.Owner);

        _studioMemberRepository.GetByUserIdWithStudioAsync(userId).Returns(new List<StudioMember> { member });

        var result = await _sut.GetStudiosForUserAsync(userId);

        Assert.That(result[0].Role, Is.EqualTo("Owner"));
    }

    [Test]
    public async Task GetStudiosForUserAsync_QueriesCorrectUserId()
    {
        var userId = Guid.NewGuid();
        _studioMemberRepository.GetByUserIdWithStudioAsync(userId).Returns(new List<StudioMember>());

        await _sut.GetStudiosForUserAsync(userId);

        await _studioMemberRepository.Received(1).GetByUserIdWithStudioAsync(userId);
    }

    private static StudioMember BuildMember(Guid userId, string studioName, long roleId,
        Guid? studioId = null) => new()
    {
        UserId = userId,
        StudioId = studioId ?? Guid.NewGuid(),
        RoleId = roleId,
        Studio = new Studio
        {
            Id = studioId ?? Guid.NewGuid(),
            Name = studioName
        },
        Role = new Role
        {
            Id = roleId,
            RoleName = roleId == Roles.Owner ? "Owner" : roleId == Roles.Manager ? "Manager" : "Employee"
        }
    };
}
