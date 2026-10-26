using Core.DTOs.Auth;
using Core.DTOs.Studio;
using Core.Interfaces.Repositories;
using Core.Models;
using Core.Validators;
using FluentValidation.TestHelper;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Unit.Validators;

// ════════════════════════════════════════════════════════════════════════════
// RegisterOwnerDtoValidator
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class RegisterOwnerDtoValidatorTests
{
    private RegisterOwnerDtoValidator _validator;

    [SetUp]
    public void SetUp() => _validator = new RegisterOwnerDtoValidator();

    [Test]
    public void Validate_PassesForValidDto()
    {
        var dto = ValidDto();
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_FailsWhenEmailEmpty()
    {
        var dto = ValidDto(); dto.Email = "";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Validate_FailsWhenEmailInvalid()
    {
        var dto = ValidDto(); dto.Email = "not-an-email";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Validate_FailsWhenPasswordEmpty()
    {
        var dto = ValidDto(); dto.Password = "";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void Validate_FailsWhenPasswordTooShort()
    {
        var dto = ValidDto(); dto.Password = "Ab1";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void Validate_FailsWhenPasswordHasNoUppercase()
    {
        var dto = ValidDto(); dto.Password = "nouppercasé1";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void Validate_FailsWhenPasswordHasNoDigit()
    {
        var dto = ValidDto(); dto.Password = "NoDigitPass!";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void Validate_FailsWhenFirstNameEmpty()
    {
        var dto = ValidDto(); dto.FirstName = "";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Test]
    public void Validate_FailsWhenFirstNameTooLong()
    {
        var dto = ValidDto(); dto.FirstName = new string('A', 51);
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Test]
    public void Validate_FailsWhenLastNameEmpty()
    {
        var dto = ValidDto(); dto.LastName = "";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Test]
    public void Validate_FailsWhenStudioNameEmpty()
    {
        var dto = ValidDto(); dto.Studio.Name = "";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Studio.Name);
    }

    [Test]
    public void Validate_FailsWhenStudioNameTooLong()
    {
        var dto = ValidDto(); dto.Studio.Name = new string('X', 101);
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Studio.Name);
    }

    [Test]
    public void Validate_PassesWhenPasswordHasExactMinLength()
    {
        var dto = ValidDto(); dto.Password = "Abcdef1!"; // exactly 8 chars
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    private static RegisterOwnerDto ValidDto() => new()
    {
        Email = "owner@test.pl",
        Password = "TestPass1!",
        FirstName = "Jan",
        LastName = "Kowalski",
        Studio = new CreateStudioDto { Name = "Detail Studio" }
    };
}

