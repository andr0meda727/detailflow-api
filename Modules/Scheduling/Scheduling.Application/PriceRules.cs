using FluentValidation;

namespace Scheduling.Application;

public static class PriceRules
{
    /// <summary>
    /// A price in złoty as the API takes it: not negative, below the system limit, at most two decimal places.
    /// </summary>
    public static IRuleBuilderOptions<T, decimal> ValidPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule
            .GreaterThanOrEqualTo(0).WithMessage("Cena nie może być ujemna.")
            .LessThan(1_000_000).WithMessage("Cena przekracza dozwolony limit systemu.")
            .Must(price => decimal.Round(price, 2) == price)
            .WithMessage("Cena może mieć co najwyżej dwa miejsca po przecinku.");

    public static long ToCents(decimal price) => (long)Math.Round(price * 100);

    public static decimal FromCents(long cents) => cents / 100m;
}
