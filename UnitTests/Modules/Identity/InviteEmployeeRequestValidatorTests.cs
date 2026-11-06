using FluentValidation.TestHelper;
using Identity.Application;
using Identity.Domain.Studios;

namespace Unit.Modules.Identity;

[TestFixture]
public class InviteEmployeeRequestValidatorTests
{
    private readonly InviteEmployeeRequestValidator _validator = new();

    [TestCase(Roles.Employee)]
    [TestCase(Roles.Manager)]
    public void Passes_ForAnAssignableRole(long roleId)
    {
        var request = new InviteEmployeeRequest { Email = "anna@example.com", RoleId = roleId };

        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [TestCase(Roles.Owner)]
    [TestCase(0L)]
    [TestCase(42L)]
    public void Rejects_OwnerMissingOrUnknownRole(long roleId)
    {
        var request = new InviteEmployeeRequest { Email = "anna@example.com", RoleId = roleId };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.RoleId);
    }

    [TestCase("")]
    [TestCase("not-an-email")]
    [TestCase(" anna@example.com")]
    [TestCase("anna@example.com ")]
    public void Rejects_MissingMalformedOrPaddedEmail(string email)
    {
        var request = new InviteEmployeeRequest { Email = email, RoleId = Roles.Employee };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Rejects_EmailLongerThan254Characters()
    {
        var request = new InviteEmployeeRequest { Email = new string('a', 250) + "@x.pl", RoleId = Roles.Employee };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Email);
    }
}
