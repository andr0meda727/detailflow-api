using FluentValidation;

namespace Shared.Application.Validation;

public static class ContactRules
{
    // Polish phone numbers: 9 digits, optionally in groups of 3 separated by spaces or dashes,
    // optionally prefixed with +48 or 0048
    private const string PhoneRegex = @"^(\+48|0048)?[\s\-]?(\d{3}[\s\-]?\d{3}[\s\-]?\d{3})$";

    public static IRuleBuilderOptions<T, string?> ValidPhone<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Matches(PhoneRegex).WithMessage(
            "Podaj prawidłowy numer telefonu (np. 500 600 700, +48500600700 lub 48-500-600-700).");
}
