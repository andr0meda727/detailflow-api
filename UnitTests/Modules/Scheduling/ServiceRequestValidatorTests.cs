using FluentValidation.TestHelper;
using Scheduling.Application.Catalog;

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
}
