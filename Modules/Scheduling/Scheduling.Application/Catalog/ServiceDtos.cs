using FluentValidation;
using Scheduling.Contracts;
using Scheduling.Domain.Catalog;
using Shared.Kernel.Vehicles;

namespace Scheduling.Application.Catalog;

/// <summary>
/// A service as staff create or edit it. Category, prices and duration are optional, so a client sending only the name
/// keeps working: a new service is then Other, quoted on request and takes an hour, and an edit keeps them. Sent prices
/// replace the whole list. <see cref="DurationMinutes"/> is the time for a Medium car.
/// </summary>
public sealed record ServiceRequest(
    string Name, ServiceCategory? Category = null, List<ServicePriceDto>? Prices = null, int? DurationMinutes = null)
{
    public IReadOnlyCollection<ServicePrice> ToPrices() =>
        (Prices ?? []).Select(p => new ServicePrice(p.VehicleSize, PriceRules.ToCents(p.Price))).ToList();
}

/// <summary>
/// The price for one vehicle size, in złoty (stored in cents).
/// </summary>
public sealed record ServicePriceDto(VehicleSize VehicleSize, decimal Price);

public sealed record ServiceResponseDto(
    Guid Id, string Name, ServiceCategory Category, List<ServicePriceDto> Prices, int DurationMinutes)
{
    public static ServiceResponseDto From(Service service) => new(
        service.Id, service.Name, service.Category,
        service.Prices.OrderBy(p => p.Size)
            .Select(p => new ServicePriceDto(p.Size, PriceRules.FromCents(p.AmountInCents)))
            .ToList(),
        service.DurationMinutes);
}

public class ServiceRequestValidator : AbstractValidator<ServiceRequest>
{
    public ServiceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nazwa usługi jest wymagana.")
            .MaximumLength(200).WithMessage("Nazwa usługi nie może przekraczać 200 znaków.");

        RuleFor(x => x.Category).IsInEnum().WithMessage("Nieznana kategoria usługi.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(Service.MinDurationMinutes, Service.MaxDurationMinutes)
            .WithMessage($"Czas trwania usługi musi wynosić od {Service.MinDurationMinutes} do {Service.MaxDurationMinutes} minut.");

        RuleFor(x => x.Prices)
            .Must(prices => prices!.Select(p => p.VehicleSize).Distinct().Count() == prices!.Count)
            .WithMessage("Każdy rozmiar pojazdu może mieć tylko jedną cenę.")
            .When(x => x.Prices != null);

        RuleForEach(x => x.Prices).ChildRules(price =>
        {
            price.RuleFor(p => p.VehicleSize).IsInEnum().WithMessage("Nieznany rozmiar pojazdu.");
            price.RuleFor(p => p.Price).ValidPrice();
        });
    }
}
