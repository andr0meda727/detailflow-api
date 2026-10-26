using Core.Constants;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.TenantIsolation;

/// <summary>
/// Tenant scoping of the legacy context. Every tenant-scoped read must return only the current studio's rows,
/// and nothing at all when no studio is resolved (fail closed). Modules have their own isolation tests.
/// </summary>
[TestFixture]
public class TenantIsolationTests
{
    private TwoStudiosSeed _seed = null!;

    [OneTimeSetUp]
    public async Task SeedAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
    }

    private ApplicationDbContext DbForStudio(Guid studioId)
    {
        var tenant = new TenantContext();
        tenant.SetTenant(studioId, Guid.NewGuid());
        return PostgresFixture.CreateDbContext(tenant);
    }

    private static ApplicationDbContext DbForUserWithoutStudio()
    {
        var tenant = new TenantContext();
        tenant.SetUserOnly(Guid.NewGuid());
        return PostgresFixture.CreateDbContext(tenant);
    }

    private StudioInvitation NewInvitation(Guid token) => new()
    {
        Email = "invitee@test.pl",
        RoleId = Roles.Employee,
        Token = token,
        Status = InvitationStatus.Pending,
        InvitedBy = _seed.StudioB.OwnerUserId,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    [Test]
    public async Task UserWithoutStudio_CannotAddTenantData()
    {
        await using var db = DbForUserWithoutStudio();

        db.StudioInvitations.Add(NewInvitation(Guid.NewGuid()));

        Assert.That(() => db.SaveChangesAsync(), Throws.InvalidOperationException);
    }

    [Test]
    public async Task Invitations_AreScopedToCurrentStudio_ButResolvableByToken()
    {
        var token = Guid.NewGuid();
        await using (var db = DbForStudio(_seed.StudioB.StudioId))
        {
            db.StudioInvitations.Add(NewInvitation(token));
            await db.SaveChangesAsync();
        }

        await using (var db = DbForStudio(_seed.StudioA.StudioId))
        {
            Assert.That(db.StudioInvitations.Any(i => i.Token == token), Is.False);
        }

        await using (var db = DbForUserWithoutStudio())
        {
            Assert.That(db.StudioInvitations.Any(i => i.Token == token), Is.False);

            // The invitee has no membership yet, so the lookup by token must work without a tenant
            var invitation = await new StudioInvitationRepository(db).GetByTokenAsync(token);
            Assert.That(invitation?.StudioId, Is.EqualTo(_seed.StudioB.StudioId));
        }
    }
}
