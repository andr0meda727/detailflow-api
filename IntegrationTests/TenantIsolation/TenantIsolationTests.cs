using Core.Constants;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Pagination;

namespace IntegrationTests.TenantIsolation;

/// <summary>
/// Every tenant-scoped read must return only the current studio's rows,
/// and nothing at all when no studio is resolved (fail closed).
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

    [Test]
    public async Task Bookings_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var bookings = (await new BookingRepository(db).GetAllWithDetailsAsync(new PageRequest())).Items;

        Assert.That(bookings.Select(b => b.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
        Assert.That(bookings.Select(b => b.Id), Does.Contain(_seed.StudioA.BookingId));
    }

    [Test]
    public async Task Clients_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var clients = (await new ClientRepository(db).GetAllWithVehiclesAsync(new PageRequest(), null)).Items;

        Assert.That(clients.Select(c => c.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
        Assert.That(clients.Select(c => c.Id), Does.Contain(_seed.StudioA.ClientId));
    }

    [Test]
    public async Task Vehicles_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var vehicles = (await new VehicleRepository(db).GetAllFilteredAsync(new PageRequest())).Items;

        Assert.That(vehicles.Select(v => v.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
    }

    [Test]
    public async Task Services_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var services = await new ServiceRepository(db).GetAllActiveAsync();

        Assert.That(services.Select(s => s.StudioId), Is.All.EqualTo(_seed.StudioA.StudioId));
    }

    [Test]
    public async Task GetById_DoesNotReturnOtherStudiosRecord()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        Assert.Multiple(async () =>
        {
            Assert.That(await new BookingRepository(db).GetByIdWithDetailsAsync(_seed.StudioB.BookingId), Is.Null);
            Assert.That(await new ClientRepository(db).GetClientByIdWithVehiclesAsync(_seed.StudioB.ClientId), Is.Null);
        });
    }

    [Test]
    public async Task UserWithoutStudio_SeesNoTenantData()
    {
        await using var db = DbForUserWithoutStudio();

        Assert.Multiple(async () =>
        {
            Assert.That((await new BookingRepository(db).GetAllWithDetailsAsync(new PageRequest())).Items, Is.Empty);
            Assert.That((await new ClientRepository(db).GetAllWithVehiclesAsync(new PageRequest(), null)).Items, Is.Empty);
            Assert.That((await new VehicleRepository(db).GetAllFilteredAsync(new PageRequest())).Items, Is.Empty);
            Assert.That(await new ServiceRepository(db).GetAllActiveAsync(), Is.Empty);
        });
    }

    [Test]
    public async Task ServiceLookup_DoesNotCountOtherStudiosServices()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);
        var ids = new[] { _seed.StudioA.ServiceId, _seed.StudioB.ServiceId };

        var count = await new ServiceRepository(db).CountExistingAsync(ids);

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task UserWithoutStudio_CannotAddTenantData()
    {
        await using var db = DbForUserWithoutStudio();

        db.Clients.Add(new Client { Name = "Anna", Phone = "500600700" });

        Assert.That(() => db.SaveChangesAsync(), Throws.InvalidOperationException);
    }

    [Test]
    public async Task SamePhone_CanBeClientOfDifferentStudios()
    {
        await using var dbA = DbForStudio(_seed.StudioA.StudioId);
        await using var dbB = DbForStudio(_seed.StudioB.StudioId);

        var clientA = await new ClientRepository(dbA).GetByIdAsync(_seed.StudioA.ClientId);
        var clientB = await new ClientRepository(dbB).GetByIdAsync(_seed.StudioB.ClientId);

        Assert.That(clientA!.Phone, Is.EqualTo(clientB!.Phone));
    }

    [Test]
    public async Task Invitations_AreScopedToCurrentStudio_ButResolvableByToken()
    {
        var token = Guid.NewGuid();
        await using (var db = DbForStudio(_seed.StudioB.StudioId))
        {
            db.StudioInvitations.Add(new StudioInvitation
            {
                Email = "invitee@test.pl",
                RoleId = Roles.Employee,
                Token = token,
                Status = InvitationStatus.Pending,
                InvitedBy = _seed.StudioB.OwnerUserId,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await db.SaveChangesAsync();
        }

        await using (var db = DbForStudio(_seed.StudioA.StudioId))
        {
            Assert.That(db.StudioInvitations.Any(i => i.Token == token), Is.False);
        }

        // The invitee has no membership yet, so the lookup must work without a tenant
        await using (var db = DbForUserWithoutStudio())
        {
            var invitation = await new StudioInvitationRepository(db).GetByTokenAsync(token);
            Assert.That(invitation?.StudioId, Is.EqualTo(_seed.StudioB.StudioId));
        }
    }
}
