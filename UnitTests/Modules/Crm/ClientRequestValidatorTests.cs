using Crm.Application.Clients;
using FluentValidation.TestHelper;

namespace Unit.Modules.Crm;

[TestFixture]
public class ClientRequestValidatorTests
{
    private readonly ClientRequestValidator _validator = new();

    private static ClientRequest Valid() => new() { Name = "Jan Kowalski", Phone = "500 600 700" };

    [Test]
    public void Passes_ForMinimalClient()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Fails_WithoutPhone()
    {
        var request = Valid();
        request.Phone = "";

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [TestCase("1234567890")]
    [TestCase("123456789")]
    public void Fails_ForInvalidNip(string nip)
    {
        var request = Valid();
        request.Nip = nip;

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Nip);
    }

    [Test]
    public void Passes_ForValidNip()
    {
        var request = Valid();
        request.Nip = "5260250274";

        _validator.TestValidate(request).ShouldNotHaveValidationErrorFor(x => x.Nip);
    }

    [Test]
    public void ValidatesEmbeddedVehiclePlate()
    {
        var request = Valid();
        request.Vehicle = new ClientVehicleRequest("KR#1", null, null);

        _validator.TestValidate(request).ShouldHaveValidationErrorFor("Vehicle.LicensePlate");
    }
}
