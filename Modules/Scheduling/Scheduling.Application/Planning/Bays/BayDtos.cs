using FluentValidation;
using Scheduling.Domain.Planning;

namespace Scheduling.Application.Planning.Bays;

public sealed record BayRequest(string Name);

public sealed record BayDto(Guid Id, string Name, bool IsActive)
{
    public static BayDto From(Bay bay) => new(bay.Id, bay.Name, bay.IsActive);
}

public class BayRequestValidator : AbstractValidator<BayRequest>
{
    public BayRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nazwa stanowiska jest wymagana.")
            .Must(name => name.Trim().Length <= Bay.MaxNameLength)
            .WithMessage($"Nazwa stanowiska nie może przekraczać {Bay.MaxNameLength} znaków.");
    }
}

public static class BayErrors
{
    public static readonly Shared.Kernel.Results.Error NotFound =
        Shared.Kernel.Results.Error.NotFound("BAY_NOT_FOUND", "Nie znaleziono stanowiska.");

    public static readonly Shared.Kernel.Results.Error HasUpcomingBookings = Shared.Kernel.Results.Error.Conflict(
        "BAY_HAS_UPCOMING_BOOKINGS", "Na tym stanowisku są zaplanowane wizyty. Przenieś je przed wyłączeniem stanowiska.");

    public static readonly Shared.Kernel.Results.Error LimitReached = Shared.Kernel.Results.Error.Conflict(
        "BAY_LIMIT_REACHED", $"Studio może mieć najwyżej {Bay.MaxActivePerStudio} aktywnych stanowisk.");
}
