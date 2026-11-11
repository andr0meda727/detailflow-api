using Identity.Application;
using Identity.Application.Abstractions;
using Identity.Application.Employees.ChangeEmployeeRole;
using Identity.Application.Employees.GetEmployees;
using Identity.Application.Employees.InviteEmployee;
using Identity.Application.Employees.RemoveEmployee;
using Identity.Application.Invitations.AcceptInvitation;
using Identity.Application.Invitations.DeclineInvitation;
using Identity.Application.Invitations.GetInvitation;
using Identity.Application.Studios.GetMyStudios;
using Identity.Application.Tenancy;
using Identity.Contracts;
using Identity.Domain.Invitations;
using Identity.Domain.Studios;
using Identity.Domain.Users;
using Identity.Infrastructure.Persistence;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.Modules.Identity;

[TestFixture]
public class IdentityHandlerTests
{
    private static readonly FrontendSettings Frontend = new("https://app.test");
    private static CancellationToken Ct => CancellationToken.None;

    private TwoStudiosSeed _seed = null!;
    private FakeTimeProvider _clock = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
        _clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
    }

    private static TenantContext TenantFor(Guid studioId, Guid userId)
    {
        var tenant = new TenantContext();
        tenant.SetTenant(studioId, userId);
        return tenant;
    }

    private IdentityDbContext OwnerDb(out TenantContext tenant)
    {
        tenant = TenantFor(_seed.StudioA.StudioId, _seed.StudioA.OwnerUserId);
        return PostgresFixture.CreateIdentityDbContext(tenant);
    }

    private static IdentityDbContext UserDb(Guid userId)
    {
        var tenant = new TenantContext();
        tenant.SetUserOnly(userId);
        return PostgresFixture.CreateIdentityDbContext(tenant);
    }

    private static async Task<User> CreateUserAsync(string? email = null)
    {
        var user = new User(Guid.NewGuid(), email ?? $"{Guid.NewGuid():N}@test.pl", "Ewa", "Nowak");
        await using var db = PostgresFixture.CreateIdentityDbContext(new TenantContext());
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<StudioInvitation> InviteAsync(string email, long roleId = Roles.Employee)
    {
        await using var db = OwnerDb(out var tenant);
        var result = await new InviteEmployeeHandler(db, tenant, Frontend, _clock)
            .HandleAsync(new InviteEmployeeRequest { Email = email, RoleId = roleId }, Ct);
        Assert.That(result.Success, Is.True, result.ErrorMessage);

        return await db.Invitations.AsNoTracking().SingleAsync(i => i.Id == result.Value, Ct);
    }

    private static async Task<List<EmployeeInvited>> InvitedEventsAsync(string email)
    {
        await using var db = PostgresFixture.CreateIdentityDbContext(new TenantContext());
        return (await db.Set<OutboxMessage>().AsNoTracking().ToListAsync(Ct))
            .Select(m => m.Deserialize()).OfType<EmployeeInvited>().Where(e => e.Email == email).ToList();
    }

    [Test]
    public async Task Invite_NewPerson_QueuesAnAccountInviteWithTheAcceptLink()
    {
        var email = $"{Guid.NewGuid():N}@test.pl";

        var invitation = await InviteAsync(email);

        var invited = (await InvitedEventsAsync(email)).Single();
        Assert.That(invited.HasAccount, Is.False);
        Assert.That(invited.AcceptUrl, Is.EqualTo($"https://app.test/accept-invitation?token={invitation.Token}"));
        Assert.That(invited.StudioName, Does.StartWith("Studio"));
    }

    [Test]
    public async Task Invite_PersonWithAnAccount_QueuesAnEmail()
    {
        var user = await CreateUserAsync();

        await InviteAsync(user.Email.ToUpperInvariant());

        Assert.That((await InvitedEventsAsync(user.Email.ToUpperInvariant())).Single().HasAccount, Is.True);
    }

    [Test]
    public async Task Invite_Twice_IsRejectedWhileTheFirstIsOpen()
    {
        var email = $"{Guid.NewGuid():N}@test.pl";
        await InviteAsync(email);

        await using var db = OwnerDb(out var tenant);
        var second = await new InviteEmployeeHandler(db, tenant, Frontend, _clock)
            .HandleAsync(new InviteEmployeeRequest { Email = email, RoleId = Roles.Employee }, Ct);

        Assert.That(second.ErrorCode, Is.EqualTo("INVITATION_PENDING"));
    }

    [Test]
    public async Task Invite_AfterADecline_ReissuesTheSameInvitation()
    {
        var user = await CreateUserAsync();
        var first = await InviteAsync(user.Email);
        await using (var db = UserDb(user.Id))
        {
            await new DeclineInvitationHandler(db, _clock).HandleAsync(first.Token, user.Id, Ct);
        }

        var second = await InviteAsync(user.Email, Roles.Manager);

        Assert.That(second.Id, Is.EqualTo(first.Id));
        Assert.That(second.Token, Is.Not.EqualTo(first.Token));
        Assert.That(second.Status, Is.EqualTo(InvitationStatus.Pending));
        Assert.That(second.RoleId, Is.EqualTo(Roles.Manager));
    }

    [Test]
    public async Task Invite_YourselfOrAMember_IsRejected()
    {
        await using var db = OwnerDb(out var tenant);
        var handler = new InviteEmployeeHandler(db, tenant, Frontend, _clock);
        var ownerEmail = $"owner-{_seed.StudioA.OwnerUserId:N}@test.pl";

        var self = await handler.HandleAsync(new InviteEmployeeRequest { Email = ownerEmail.ToUpperInvariant(), RoleId = Roles.Employee }, Ct);

        Assert.That(self.ErrorCode, Is.EqualTo("SELF_INVITE"));
    }

    [Test]
    public async Task Accept_JoinsTheStudioWithTheInvitedRole()
    {
        var user = await CreateUserAsync();
        var invitation = await InviteAsync(user.Email, Roles.Manager);

        await using (var db = UserDb(user.Id))
        {
            var result = await new AcceptInvitationHandler(db, _clock).HandleAsync(invitation.Token, user.Id, Ct);
            Assert.That(result.Success, Is.True, result.ErrorMessage);
        }

        await using var readDb = UserDb(user.Id);
        var access = await new ResolveCallerHandler(readDb).HandleAsync(user.Id, Ct);
        Assert.That(access.Memberships, Is.EqualTo(new[] { new ActiveMembership(_seed.StudioA.StudioId, Roles.Manager) }));
    }

    [Test]
    public async Task RemovedEmployee_CanRejoinThroughANewInvitation()
    {
        var user = await CreateUserAsync();
        var invitation = await InviteAsync(user.Email);
        await using (var db = UserDb(user.Id))
        {
            await new AcceptInvitationHandler(db, _clock).HandleAsync(invitation.Token, user.Id, Ct);
        }
        await using (var db = OwnerDb(out var tenant))
        {
            Assert.That((await new RemoveEmployeeHandler(db, tenant).HandleAsync(user.Id, Ct)).Success, Is.True);
        }

        var again = await InviteAsync(user.Email);
        await using (var db = UserDb(user.Id))
        {
            Assert.That((await new AcceptInvitationHandler(db, _clock).HandleAsync(again.Token, user.Id, Ct)).Success, Is.True);
        }

        await using var readDb = UserDb(user.Id);
        var studios = await new GetMyStudiosHandler(readDb).HandleAsync(user.Id, Ct);
        Assert.That(studios.Select(s => s.StudioId), Is.EqualTo(new[] { _seed.StudioA.StudioId }));
    }

    [Test]
    public async Task RemovedEmployee_DisappearsFromStudiosAndStaffLists()
    {
        var user = await CreateUserAsync();
        var invitation = await InviteAsync(user.Email);
        await using (var db = UserDb(user.Id))
        {
            await new AcceptInvitationHandler(db, _clock).HandleAsync(invitation.Token, user.Id, Ct);
        }

        await using (var db = OwnerDb(out var tenant))
        {
            await new RemoveEmployeeHandler(db, tenant).HandleAsync(user.Id, Ct);
        }

        await using var readDb = OwnerDb(out var readTenant);
        Assert.That(await new GetMyStudiosHandler(readDb).HandleAsync(user.Id, Ct), Is.Empty);
        Assert.That((await new GetEmployeesHandler(readDb, readTenant).HandleAsync(Ct)).Select(e => e.UserId),
            Is.EqualTo(new[] { _seed.StudioA.OwnerUserId }));
    }

    [Test]
    public async Task ChangeRole_OfTheOwner_IsRejected()
    {
        await using var db = OwnerDb(out var tenant);

        var result = await new ChangeEmployeeRoleHandler(db, tenant)
            .HandleAsync(new ChangeEmployeeRoleRequest(_seed.StudioA.OwnerUserId, Roles.Employee), Ct);

        Assert.That(result.ErrorCode, Is.EqualTo("SELF_ROLE_CHANGE"));
    }

    [Test]
    public async Task Accept_ByAnotherAccount_IsForbidden()
    {
        var invitation = await InviteAsync($"{Guid.NewGuid():N}@test.pl");
        var someoneElse = await CreateUserAsync();

        await using var db = UserDb(someoneElse.Id);
        var result = await new AcceptInvitationHandler(db, _clock).HandleAsync(invitation.Token, someoneElse.Id, Ct);

        Assert.That(result.ErrorCode, Is.EqualTo("WRONG_USER"));
    }

    [Test]
    public async Task GetInvitation_IsFoundByTokenWithoutATenant_UntilItExpires()
    {
        var invitation = await InviteAsync($"{Guid.NewGuid():N}@test.pl");

        await using var db = PostgresFixture.CreateIdentityDbContext(new TenantContext());
        var open = await new GetInvitationHandler(db, _clock).HandleAsync(invitation.Token, Ct);
        _clock.Advance(StudioInvitation.ValidFor);
        var expired = await new GetInvitationHandler(db, _clock).HandleAsync(invitation.Token, Ct);

        Assert.That(open!.Role, Is.EqualTo("Employee"));
        Assert.That(expired, Is.Null);
        Assert.That((await db.FindInvitationByTokenAsync(invitation.Token, Ct))!.Status, Is.EqualTo(InvitationStatus.Expired));
    }

    [Test]
    public async Task Invitations_AreScopedToTheirStudio()
    {
        var invitation = await InviteAsync($"{Guid.NewGuid():N}@test.pl");

        var tenant = TenantFor(_seed.StudioB.StudioId, _seed.StudioB.OwnerUserId);
        await using var otherStudio = PostgresFixture.CreateIdentityDbContext(tenant);

        Assert.That(await otherStudio.Invitations.AnyAsync(i => i.Id == invitation.Id, Ct), Is.False);
    }
}
