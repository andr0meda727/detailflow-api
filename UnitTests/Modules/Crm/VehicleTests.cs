using Crm.Domain;
using Shared.Kernel.Vehicles;

namespace Unit.Modules.Crm;

[TestFixture]
public class VehicleTests
{
    [Test]
    public void Create_NormalizesLicensePlateAndBlankFields()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), " kr 12345 ", " ", "A4");

        Assert.That(vehicle.LicensePlate, Is.EqualTo("KR 12345"));
        Assert.That(vehicle.Brand, Is.Null);
        Assert.That(vehicle.Model, Is.EqualTo("A4"));
    }

    [Test]
    public void Create_RequiresClientAndPlate()
    {
        Assert.That(() => Vehicle.Create(Guid.Empty, "KR 1", null, null), Throws.ArgumentException);
        Assert.That(() => Vehicle.Create(Guid.NewGuid(), "", null, null), Throws.ArgumentException);
    }

    [Test]
    public void Update_CanMoveVehicleToAnotherClient()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "KR 1", null, null);
        var newOwner = Guid.NewGuid();

        vehicle.Update(newOwner, "KR 1", "Audi", null, VehicleSize.Medium);

        Assert.That(vehicle.ClientId, Is.EqualTo(newOwner));
        Assert.That(vehicle.Brand, Is.EqualTo("Audi"));
    }

    [Test]
    public void Create_WithoutASize_IsMedium()
    {
        Assert.That(Vehicle.Create(Guid.NewGuid(), "KR 1", null, null).Size, Is.EqualTo(VehicleSize.Medium));
    }

    [Test]
    public void Update_ChangesTheSizeAndRejectsUnknownOnes()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "KR 1", null, null);

        vehicle.Update(vehicle.ClientId, "KR 1", "Volvo", "XC90", VehicleSize.Large);

        Assert.That(vehicle.Size, Is.EqualTo(VehicleSize.Large));
        Assert.That(() => vehicle.Update(vehicle.ClientId, "KR 1", null, null, (VehicleSize)0),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }
}
