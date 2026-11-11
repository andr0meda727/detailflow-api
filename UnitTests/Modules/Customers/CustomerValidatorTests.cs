using Customers.Application;
using FluentValidation.TestHelper;
using Shared.Kernel.Vehicles;

namespace Unit.Modules.Customers;

[TestFixture]
public class CustomerValidatorTests
{
    private readonly ProfileRequestValidator _profile = new();
    private readonly GarageVehicleRequestValidator _vehicle = new();

    [TestCase("500 600 700")]
    [TestCase("+48500600700")]
    public void Profile_AcceptsAPolishPhone(string phone)
    {
        _profile.TestValidate(new ProfileRequest("Anna", phone)).ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("")]
    [TestCase("12345")]
    public void Profile_RejectsAMissingOrInvalidPhone(string phone)
    {
        _profile.TestValidate(new ProfileRequest("Anna", phone)).ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Test]
    public void Vehicle_Passes_WithPlateAndSize()
    {
        _vehicle.TestValidate(new GarageVehicleRequest("KR 12345", null, null, VehicleSize.Small))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Vehicle_NeedsASize()
    {
        _vehicle.TestValidate(new GarageVehicleRequest("KR 12345", null, null, null)).ShouldHaveValidationErrorFor(x => x.Size);
    }

    [TestCase("")]
    [TestCase("KR#1")]
    [TestCase("ABCDEFGHIJKLMNOP")]
    public void Vehicle_RejectsABadPlate(string plate)
    {
        _vehicle.TestValidate(new GarageVehicleRequest(plate, null, null, VehicleSize.Small))
            .ShouldHaveValidationErrorFor(x => x.LicensePlate);
    }
}
