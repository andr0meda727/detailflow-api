using Crm.Application.Directory;
using Crm.Contracts;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.Modules.Crm;

/// <summary>
/// When staff accept an online request, CRM finds or creates the studio's client and car for that customer.
/// </summary>
[TestFixture]
public class OnlineCustomerTests
{
    private TwoStudiosSeed _seed = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
    }

    private TenantContext StudioA()
    {
        var tenant = new TenantContext();
        tenant.SetTenant(_seed.StudioA.StudioId, _seed.StudioA.OwnerUserId);
        return tenant;
    }

    private static OnlineCustomer Anna(Guid userId, string phone, string plate = "KR 777") =>
        new(userId, "Anna Nowak", phone, "anna@example.com", plate, "Volvo", "XC90", "Large");

    private async Task<BookingParties> EnsureAsync(OnlineCustomer customer)
    {
        await using var db = PostgresFixture.CreateCrmDbContext(StudioA());
        return await new ClientDirectory(db).EnsureOnlineCustomerAsync(customer);
    }

    [Test]
    public async Task ANewCustomer_BecomesALinkedClientWithTheirCar_OnlyOnce()
    {
        var userId = Guid.NewGuid();
        var phone = Random.Shared.Next(100_000_000, 999_999_999).ToString();

        var first = await EnsureAsync(Anna(userId, phone));
        var again = await EnsureAsync(Anna(userId, phone));

        Assert.That(again.Client.Id, Is.EqualTo(first.Client.Id));
        Assert.That(again.Vehicle.Id, Is.EqualTo(first.Vehicle.Id));
        Assert.That(first.Vehicle.Size, Is.EqualTo("Large"));

        await using var db = PostgresFixture.CreateCrmDbContext(StudioA());
        var client = await db.Clients.Include(c => c.Vehicles).SingleAsync(c => c.Id == first.Client.Id);
        Assert.That(client.CustomerUserId, Is.EqualTo(userId));
        Assert.That(client.Vehicles, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AClientTheStudioAlreadyHas_IsLinkedByPhone_AndGetsTheNewCar()
    {
        // The seeded client of studio A has this phone and no account
        var userId = Guid.NewGuid();

        var parties = await EnsureAsync(Anna(userId, _seed.SharedClientPhone, "KR NEW"));

        Assert.That(parties.Client.Id, Is.EqualTo(_seed.StudioA.ClientId));
        Assert.That(parties.Vehicle.Id, Is.Not.EqualTo(_seed.StudioA.VehicleId));
        Assert.That(parties.Vehicle.LicensePlate, Is.EqualTo("KR NEW"));

        // Next time the account finds the same client directly, even with another phone
        var later = await EnsureAsync(Anna(userId, "999888777", "KR NEW"));
        Assert.That(later.Client.Id, Is.EqualTo(_seed.StudioA.ClientId));
        Assert.That(later.Vehicle.Id, Is.EqualTo(parties.Vehicle.Id));
    }
}
