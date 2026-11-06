using Scheduling.Domain.Catalog;

namespace Unit.Modules.Scheduling;

[TestFixture]
public class ServiceTests
{
    [Test]
    public void Create_IsActiveWithATrimmedName()
    {
        var service = Service.Create("  Polerowanie ");

        Assert.That(service.Name, Is.EqualTo("Polerowanie"));
        Assert.That(service.IsActive, Is.True);
        Assert.That(service.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_RejectsBlankName(string name)
    {
        Assert.That(() => Service.Create(name), Throws.ArgumentException);
    }

    [Test]
    public void Deactivate_HidesItFromNewBookings()
    {
        var service = Service.Create("Woskowanie");

        service.Deactivate();

        Assert.That(service.IsActive, Is.False);
    }
}
