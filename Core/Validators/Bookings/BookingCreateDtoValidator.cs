using Core.DTOs.Bookings;
using Core.Interfaces.Repositories;
using FluentValidation;

namespace Core.Validators.Bookings;

public class BookingCreateDtoValidator : AbstractValidator<BookingCreateDto>
{
    private readonly IVehicleRepository _vehicleRepository;

    // Bookings cannot be scheduled further than 2 years into the future
    private static readonly TimeSpan MaxFutureBooking = TimeSpan.FromDays(365 * 2);

    public BookingCreateDtoValidator(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;

        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("ID klienta jest wymagane.");

        RuleFor(x => x.StatusId)
            .NotEmpty().WithMessage("Musisz wybrać status rezerwacji."); // Chyba ze bez wyboru robimy

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notatki są za długie (maksymalnie 2000 znaków).")
            .Must(n => n!.Trim().Length > 0)
            .WithMessage("Notatki nie mogą składać się tylko z białych znaków.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Cena nie może być ujemna.")
            .LessThan(1_000_000).WithMessage("Cena przekracza dozwolony limit systemu.")
            .Must(HaveAtMostTwoDecimalPlaces)
            .WithMessage("Cena może mieć co najwyżej dwa miejsca po przecinku.");

        RuleFor(x => x.ServiceIds)
            .NotEmpty().WithMessage("Musisz wybrać przynajmniej jedną usługę.")
            .Must(ids => ids.Count <= 30).WithMessage("Możesz wybrać maksymalnie 30 usług na raz.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Lista usług zawiera duplikaty.");


        RuleFor(x => x.ScheduledAt)
            .NotEmpty().WithMessage("Data planowanej rezerwacji jest wymagana.")
            .GreaterThan(DateTime.UtcNow.AddMinutes(-10))
            .WithMessage("Data rezerwacji nie może być w przeszłości.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Add(MaxFutureBooking))
            .WithMessage("Rezerwacja nie może być zaplanowana na więcej niż 2 lata do przodu.");


        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Musisz podać ID pojazdu.")
            .MustAsync(BeOwnedByClientAsync)
            .WithMessage("Wybrany pojazd nie istnieje lub nie należy do podanego klienta.");
    }

    private static bool HaveAtMostTwoDecimalPlaces(decimal price)
    {
        return decimal.Round(price, 2) == price;
    }

    private async Task<bool> BeOwnedByClientAsync(BookingCreateDto dto, Guid vehicleId, CancellationToken cancellationToken)
    {
        if (dto.ClientId == Guid.Empty || vehicleId == Guid.Empty) return false;

        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);

        return vehicle != null && vehicle.ClientId == dto.ClientId;
    }
}