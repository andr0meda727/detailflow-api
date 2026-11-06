using Customers.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Vehicles;

namespace IntegrationTests.Modules.Customers;

/// <summary>
/// Customer data is per account: the Owner filter shows only the signed-in user's rows, and nothing without a user.
/// </summary>
[TestFixture]
public class CustomerOwnershipTests
{
    private static TenantContext As(Guid? userId)
    {
        var tenant = new TenantContext();
        if (userId is { } id) tenant.SetUserOnly(id);
        return tenant;
    }

    [Test]
    public async Task EachUserSeesOnlyTheirOwnData_AndNobodySeesItWithoutSigningIn()
    {
        var anna = Guid.NewGuid();
        var piotr = Guid.NewGuid();
        await using (var db = PostgresFixture.CreateCustomersDbContext(As(anna)))
        {
            db.Profiles.Add(CustomerProfile.Create(anna, "Anna", "500600700"));
            db.Vehicles.Add(CustomerVehicle.Create(anna, "KR 1", null, null, VehicleSize.Small));
            await db.SaveChangesAsync();
        }

        await using var asAnna = PostgresFixture.CreateCustomersDbContext(As(anna));
        await using var asPiotr = PostgresFixture.CreateCustomersDbContext(As(piotr));
        await using var anonymous = PostgresFixture.CreateCustomersDbContext(As(null));

        Assert.Multiple(async () =>
        {
            Assert.That(await asAnna.Vehicles.CountAsync(), Is.EqualTo(1));
            Assert.That(await asAnna.Profiles.AnyAsync(p => p.UserId == anna), Is.True);
            Assert.That(await asPiotr.Vehicles.AnyAsync(), Is.False);
            Assert.That(await asPiotr.Profiles.AnyAsync(p => p.UserId == anna), Is.False);
            Assert.That(await anonymous.Vehicles.AnyAsync(), Is.False);
            Assert.That(await anonymous.Profiles.AnyAsync(), Is.False);
        });
    }
}
