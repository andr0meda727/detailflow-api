using FluentValidation.TestHelper;
using Scheduling.Application.Catalog;
using Scheduling.Contracts;
using Shared.Kernel.Vehicles;

namespace Unit.Modules.Scheduling;

[TestFixture]
public class ServiceRequestValidatorTests
{
    private readonly ServiceRequestValidator _validator = new();

    [Test]
    public void Passes_ForANormalName()
    {
        _validator.TestValidate(new ServiceRequest("Polerowanie")).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Passes_AtTheLengthLimit()
    {
        _validator.TestValidate(new ServiceRequest(new string('a', 200))).ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Rejects_BlankName(string name)
    {
        _validator.TestValidate(new ServiceRequest(name)).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Rejects_NameOver200Characters()
    {
        _validator.TestValidate(new ServiceRequest(new string('a', 201))).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Passes_WithACategoryAndPricesPerSize()
    {
        var request = new ServiceRequest("Pranie tapicerki", ServiceCategory.Interior,
            [new ServicePriceDto(VehicleSize.Small, 249.99m), new ServicePriceDto(VehicleSize.XL, 499m)]);

        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Rejects_AnUnknownCategory()
    {
        _validator.TestValidate(new ServiceRequest("X", (ServiceCategory)42)).ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Test]
    public void Rejects_TwoPricesForOneSize()
    {
        var request = new ServiceRequest("X", ServiceCategory.Other,
            [new ServicePriceDto(VehicleSize.Small, 100m), new ServicePriceDto(VehicleSize.Small, 120m)]);

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Prices);
    }

    [TestCase(-1)]
    [TestCase(10.555)]
    [TestCase(1_000_000)]
    public void Rejects_AnInvalidPrice(decimal price)
    {
        var request = new ServiceRequest("X", ServiceCategory.Other, [new ServicePriceDto(VehicleSize.Small, price)]);

        _validator.TestValidate(request).ShouldHaveValidationErrorFor("Prices[0].Price");
    }

    [Test]
    public void Rejects_AnUnknownVehicleSize()
    {
        var request = new ServiceRequest("X", ServiceCategory.Other, [new ServicePriceDto((VehicleSize)0, 100m)]);

        _validator.TestValidate(request).ShouldHaveValidationErrorFor("Prices[0].VehicleSize");
    }
}
