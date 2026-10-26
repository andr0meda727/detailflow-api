using Crm.Domain;

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

        vehicle.Update(newOwner, "KR 1", "Audi", null);

        Assert.That(vehicle.ClientId, Is.EqualTo(newOwner));
        Assert.That(vehicle.Brand, Is.EqualTo("Audi"));
    }
}
