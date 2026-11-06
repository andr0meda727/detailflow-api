using Customers.Domain;
using Customers.Infrastructure.Directory;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Vehicles;

namespace IntegrationTests.Modules.Customers;

/// <summary>
/// Other modules read a customer's data by account ID, whoever is calling.
/// </summary>
[TestFixture]
public class CustomerDirectoryTests
{
    [Test]
    public async Task FindsTheAccountsProfileAndCar_ButNotAnotherAccountsCar()
    {
        var anna = Guid.NewGuid();
        var annasCar = CustomerVehicle.Create(anna, "KR 1", "Volvo", "XC90", VehicleSize.Large);
        var signedInAsAnna = new TenantContext();
        signedInAsAnna.SetUserOnly(anna);
        await using (var db = PostgresFixture.CreateCustomersDbContext(signedInAsAnna))
        {
            db.Profiles.Add(CustomerProfile.Create(anna, "Anna", "500600700"));
            db.Vehicles.Add(annasCar);
            await db.SaveChangesAsync();
        }

        // E.g. Scheduling handling the request, with no customer filter in its way
        await using var other = PostgresFixture.CreateCustomersDbContext(new TenantContext());
        var directory = new CustomerDirectory(other);

        var profile = await directory.FindProfileAsync(anna);
        var car = await directory.FindVehicleAsync(anna, annasCar.Id);
        var asSomeoneElse = await directory.FindVehicleAsync(Guid.NewGuid(), annasCar.Id);

        Assert.That(profile?.Name, Is.EqualTo("Anna"));
        Assert.That(car?.Size, Is.EqualTo("Large"));
        Assert.That(asSomeoneElse, Is.Null);
        Assert.That(await directory.FindProfileAsync(Guid.NewGuid()), Is.Null);
    }
}
