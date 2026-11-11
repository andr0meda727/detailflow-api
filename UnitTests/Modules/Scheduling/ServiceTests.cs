using Scheduling.Contracts;
using Scheduling.Domain.Catalog;
using Shared.Kernel.Vehicles;

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

    [Test]
    public void Create_WithoutDetails_IsOtherAndQuotedOnRequest()
    {
        var service = Service.Create("Woskowanie");

        Assert.That(service.Category, Is.EqualTo(ServiceCategory.Other));
        Assert.That(service.Prices, Is.Empty);
        Assert.That(service.PriceFor(VehicleSize.Medium), Is.Null);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_RejectsBlankName(string name)
    {
        Assert.That(() => Service.Create(name), Throws.ArgumentException);
    }

    [Test]
    public void PriceFor_ReturnsThePriceOfThatSize()
    {
        var service = Service.Create("Pranie tapicerki", ServiceCategory.Interior,
            [new ServicePrice(VehicleSize.Small, 25000), new ServicePrice(VehicleSize.Large, 40000)]);

        Assert.That(service.PriceFor(VehicleSize.Small), Is.EqualTo(25000));
        Assert.That(service.PriceFor(VehicleSize.Large), Is.EqualTo(40000));
        Assert.That(service.PriceFor(VehicleSize.XL), Is.Null);
    }

    [Test]
    public void Update_ReplacesTheWholePriceList()
    {
        var service = Service.Create("Powłoka", ServiceCategory.Ceramic,
            [new ServicePrice(VehicleSize.Small, 100000), new ServicePrice(VehicleSize.Medium, 120000)]);

        service.Update(" Powłoka 3 lata ", ServiceCategory.Ceramic, [new ServicePrice(VehicleSize.Large, 150000)]);

        Assert.That(service.Name, Is.EqualTo("Powłoka 3 lata"));
        Assert.That(service.Prices, Is.EqualTo(new[] { new ServicePrice(VehicleSize.Large, 150000) }));
    }

    [Test]
    public void Update_RejectsTwoPricesForOneSize()
    {
        var service = Service.Create("Mycie");

        Assert.That(() => service.Update("Mycie", ServiceCategory.ExteriorWash,
            [new ServicePrice(VehicleSize.Small, 5000), new ServicePrice(VehicleSize.Small, 6000)]), Throws.ArgumentException);
    }

    [Test]
    public void Update_RejectsNegativePricesAndUnknownValues()
    {
        var service = Service.Create("Mycie");

        Assert.Multiple(() =>
        {
            Assert.That(() => service.Update("Mycie", ServiceCategory.ExteriorWash, [new ServicePrice(VehicleSize.Small, -1)]),
                Throws.ArgumentException);
            Assert.That(() => service.Update("Mycie", ServiceCategory.ExteriorWash, [new ServicePrice((VehicleSize)42, 100)]),
                Throws.ArgumentException);
            Assert.That(() => service.Update("Mycie", (ServiceCategory)42, []), Throws.InstanceOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void Deactivate_HidesItFromNewBookings()
    {
        var service = Service.Create("Woskowanie");

        service.Deactivate();

        Assert.That(service.IsActive, Is.False);
    }

    [Test]
    public void Create_WithoutDuration_TakesAnHour()
    {
        Assert.That(Service.Create("Mycie").DurationMinutes, Is.EqualTo(60));
    }

    [Test]
    public void Update_WithoutDuration_KeepsIt()
    {
        var service = Service.Create("Mycie", durationMinutes: 90);

        service.Update("Mycie premium", ServiceCategory.ExteriorWash, []);

        Assert.That(service.DurationMinutes, Is.EqualTo(90));
    }

    [TestCase(4)]
    [TestCase(721)]
    public void Create_RejectsDurationOutOfRange(int minutes)
    {
        Assert.That(() => Service.Create("Mycie", durationMinutes: minutes), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }
}
