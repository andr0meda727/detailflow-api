using Core.DTOs.Vehicles;
using FluentValidation;

namespace Core.Validators.Clients;

/// <summary>
/// Validates the optional vehicle embedded in POST /api/clients.
/// Supports international license plates.
/// </summary>
public class CreateClientVehicleDtoValidator : AbstractValidator<CreateClientVehicleDto>
{
    // International permissive pattern:
    // \p{L} matches any kind of letter from any language.
    // 0-9 matches digits, \- matches hyphens, \s matches spaces.
    private const string InternationalLicensePlateRegex = @"^[\p{L}0-9\-\s]+$";

    public CreateClientVehicleDtoValidator()
    {
        RuleFor(x => x.LicensePlate)
            .NotEmpty().WithMessage("Numer rejestracyjny pojazdu jest wymagany.")
            .MaximumLength(15).WithMessage("Numer rejestracyjny nie może przekraczać 15 znaków.")
            .Matches(InternationalLicensePlateRegex).WithMessage(
                "Numer rejestracyjny zawiera niedozwolone znaki. Dozwolone są tylko litery, cyfry, spacje i myślniki.");

        RuleFor(x => x.Brand)
            .MaximumLength(50).WithMessage("Marka pojazdu nie może przekraczać 100 znaków.")

        RuleFor(x => x.Model)
            .MaximumLength(50).WithMessage("Model pojazdu nie może przekraczać 100 znaków.")
    }
}