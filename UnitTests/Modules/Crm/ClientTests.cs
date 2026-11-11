using Crm.Domain;

namespace Unit.Modules.Crm;

[TestFixture]
public class ClientTests
{
    [Test]
    public void Create_TrimsAndStoresDetails()
    {
        var client = Client.Create(" Jan Kowalski ", " 500600700 ", " jan@test.pl ", " ");

        Assert.Multiple(() =>
        {
            Assert.That(client.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(client.Name, Is.EqualTo("Jan Kowalski"));
            Assert.That(client.Phone, Is.EqualTo("500600700"));
            Assert.That(client.Email, Is.EqualTo("jan@test.pl"));
            Assert.That(client.Nip, Is.Null);
            Assert.That(client.IsDeleted, Is.False);
        });
    }

    [TestCase("", "500600700")]
    [TestCase("Jan", " ")]
    public void Create_RequiresNameAndPhone(string name, string phone)
    {
        Assert.That(() => Client.Create(name, phone, null, null), Throws.ArgumentException);
    }

    [Test]
    public void AddVehicle_LinksVehicleToClient()
    {
        var client = Client.Create("Jan", "500600700", null, null);

        var vehicle = client.AddVehicle("kr 12345", "BMW", "3");

        Assert.That(client.Vehicles, Is.EquivalentTo(new[] { vehicle }));
        Assert.That(vehicle.ClientId, Is.EqualTo(client.Id));
    }

    [Test]
    public void AddVehicle_IsRejectedForDeletedClient()
    {
        var client = Client.Create("Jan", "500600700", null, null);
        client.Delete();

        Assert.That(() => client.AddVehicle("KR 1", null, null), Throws.InvalidOperationException);
    }
}
