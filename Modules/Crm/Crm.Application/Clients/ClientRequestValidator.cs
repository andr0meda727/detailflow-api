using FluentValidation;
using Shared.Application.Validation;

namespace Crm.Application.Clients;

public class ClientRequestValidator : AbstractValidator<ClientRequest>
{
    private const string NipRegex = @"^\d{10}$";

    public ClientRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Imię/nazwa klienta jest wymagana.")
            .MaximumLength(200).WithMessage("Imię/nazwa klienta nie może przekraczać 200 znaków.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Imię/nazwa klienta nie może składać się tylko z białych znaków.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Podany adres e-mail jest nieprawidłowy.")
            .MaximumLength(254).WithMessage("Adres e-mail jest zbyt długi.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        // The phone identifies the client within the studio, and the database requires it
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Numer telefonu klienta jest wymagany.")
            .ValidPhone();

        RuleFor(x => x.Nip)
            .Matches(NipRegex).WithMessage("NIP musi składać się z dokładnie 10 cyfr.")
            .Must(BeValidNip).WithMessage("Podany NIP ma nieprawidłową cyfrę kontrolną.")
            .When(x => !string.IsNullOrEmpty(x.Nip));

        RuleFor(x => x.Vehicle!.LicensePlate).ValidLicensePlate().When(x => x.Vehicle != null);
        RuleFor(x => x.Vehicle!.Brand).ValidBrandOrModel("Marka").When(x => x.Vehicle != null);
        RuleFor(x => x.Vehicle!.Model).ValidBrandOrModel("Model").When(x => x.Vehicle != null);
        RuleFor(x => x.Vehicle!.Size).ValidVehicleSize().When(x => x.Vehicle != null);
    }

    /// <summary>
    /// Polish NIP check digit: weights 6, 5, 7, 2, 3, 4, 5, 6, 7 on digits 1–9, sum mod 11 must equal digit 10.
    /// </summary>
    private static bool BeValidNip(string? nip)
    {
        if (string.IsNullOrEmpty(nip) || nip.Length != 10 || !nip.All(char.IsDigit)) return false;

        int[] weights = [6, 5, 7, 2, 3, 4, 5, 6, 7];
        var digits = nip.Select(c => c - '0').ToArray();
        var checkDigit = weights.Select((w, i) => w * digits[i]).Sum() % 11;

        // A remainder of 10 is never a valid NIP
        return checkDigit != 10 && checkDigit == digits[9];
    }
}
