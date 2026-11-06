using FluentValidation;
using Shared.Kernel.Vehicles;

namespace Shared.Application.Validation;

/// <summary>
/// Vehicle rules shared by every place a car is entered: a studio's client cars (CRM) and a customer's garage.
/// </summary>
public static class VehicleRules
{
    // \p{L} matches letters of any alphabet, so foreign plates are accepted too
    private const string LicensePlateRegex = @"^[\p{L}0-9\-\s]+$";

    public static IRuleBuilderOptions<T, string> ValidLicensePlate<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Numer rejestracyjny pojazdu jest wymagany.")
            .MaximumLength(15).WithMessage("Numer rejestracyjny nie może przekraczać 15 znaków.")
            .Matches(LicensePlateRegex).WithMessage(
                "Numer rejestracyjny zawiera niedozwolone znaki. Dozwolone są tylko litery, cyfry, spacje i myślniki.");

    public static IRuleBuilderOptions<T, VehicleSize?> ValidVehicleSize<T>(this IRuleBuilder<T, VehicleSize?> rule) =>
        rule.IsInEnum().WithMessage("Nieznany rozmiar pojazdu.");

    public static IRuleBuilderOptions<T, string?> ValidBrandOrModel<T>(this IRuleBuilder<T, string?> rule, string field) =>
        rule.MaximumLength(50).WithMessage($"{field} pojazdu nie może przekraczać 50 znaków.");
}
