using Core.DTOs.Clients;
using FluentValidation;

namespace Core.Validators.Clients;

public class CreateClientDtoValidator : AbstractValidator<CreateClientDto>
{
    // Polish phone number patterns:
    //   - 9 digits optionally separated by spaces, dots or dashes in groups of 3
    //   - Optionally prefixed with country code +48 or 0048
    private const string PhoneRegex =
        @"^(\+48|0048)?[\s\-]?(\d{3}[\s\-]?\d{3}[\s\-]?\d{3})$";

    // NIP: exactly 10 digits (we also verify the check digit)
    private const string NipRegex = @"^\d{10}$";

    public CreateClientDtoValidator()
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

        RuleFor(x => x.Phone)
            .Matches(PhoneRegex).WithMessage(
                "Podaj prawidłowy numer telefonu (np. 500 600 700, +48500600700 lub 48-500-600-700).")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Nip)
            .Matches(NipRegex).WithMessage("NIP musi składać się z dokładnie 10 cyfr.")
            .Must(BeValidNip).WithMessage("Podany NIP ma nieprawidłową cyfrę kontrolną.")
            .When(x => !string.IsNullOrEmpty(x.Nip));

        RuleFor(x => x.Vehicle)
            .SetValidator(new CreateClientVehicleDtoValidator()!)
            .When(x => x.Vehicle != null);
    }

    /// <summary>
    /// Verifies the NIP check digit using the standard Polish algorithm.
    /// Weights: 6, 5, 7, 2, 3, 4, 5, 6, 7 applied to digits 1–9, remainder mod 11 must equal digit 10.
    /// </summary>
    private static bool BeValidNip(string? nip)
    {
        if (string.IsNullOrEmpty(nip) || nip.Length != 10) return false;
        if (!nip.All(char.IsDigit)) return false;

        int[] weights = [6, 5, 7, 2, 3, 4, 5, 6, 7];
        var digits = nip.Select(c => c - '0').ToArray();
        var sum = weights.Select((w, i) => w * digits[i]).Sum();
        var checkDigit = sum % 11;

        // mod 11 == 10 means invalid NIP
        if (checkDigit == 10) return false;

        return checkDigit == digits[9];
    }
}