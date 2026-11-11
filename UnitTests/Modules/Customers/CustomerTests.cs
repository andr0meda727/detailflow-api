using Customers.Domain;
using Shared.Kernel.Vehicles;

namespace Unit.Modules.Customers;

[TestFixture]
public class CustomerTests
{
    private static readonly Guid User = Guid.NewGuid();

    [Test]
    public void Profile_TrimsNameAndPhone()
    {
        var profile = CustomerProfile.Create(User, " Anna Nowak ", " 500 600 700 ");

        Assert.That(profile.Name, Is.EqualTo("Anna Nowak"));
        Assert.That(profile.Phone, Is.EqualTo("500 600 700"));
    }

    [Test]
    public void Profile_NeedsAnAccountNameAndPhone()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => CustomerProfile.Create(Guid.Empty, "Anna", "500600700"), Throws.ArgumentException);
            Assert.That(() => CustomerProfile.Create(User, " ", "500600700"), Throws.ArgumentException);
            Assert.That(() => CustomerProfile.Create(User, "Anna", ""), Throws.ArgumentException);
        });
    }

    [Test]
    public void Vehicle_NormalizesThePlateLikeCrm()
    {
        var vehicle = CustomerVehicle.Create(User, " kr 12345 ", " ", "XC90", VehicleSize.Large);

        Assert.That(vehicle.LicensePlate, Is.EqualTo("KR 12345"));
        Assert.That(vehicle.Brand, Is.Null);
        Assert.That(vehicle.Size, Is.EqualTo(VehicleSize.Large));
    }

    [Test]
    public void Vehicle_RejectsAnUnknownSize()
    {
        Assert.That(() => CustomerVehicle.Create(User, "KR 1", null, null, (VehicleSize)0),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Vehicle_UpdateChangesEverythingButTheOwner()
    {
        var vehicle = CustomerVehicle.Create(User, "KR 1", "Fiat", "500", VehicleSize.Small);

        vehicle.Update("KR 2", "Volvo", "XC60", VehicleSize.Large);

        Assert.That(vehicle.UserId, Is.EqualTo(User));
        Assert.That(vehicle.LicensePlate, Is.EqualTo("KR 2"));
        Assert.That(vehicle.Size, Is.EqualTo(VehicleSize.Large));
    }
}
