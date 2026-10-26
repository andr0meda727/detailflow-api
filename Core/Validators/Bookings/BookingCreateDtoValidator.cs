using Core.DTOs.Bookings;
using Core.Interfaces.Repositories;
using Crm.Contracts;
using FluentValidation;

namespace Core.Validators.Bookings;

public class BookingCreateDtoValidator : AbstractValidator<BookingCreateDto>
{
    private readonly IClientDirectory _clientDirectory;
    private readonly IServiceRepository _serviceRepository;

    // Bookings cannot be scheduled further than 2 years into the future
    private static readonly TimeSpan MaxFutureBooking = TimeSpan.FromDays(365 * 2);

    public BookingCreateDtoValidator(IClientDirectory clientDirectory, IServiceRepository serviceRepository)
    {
        _clientDirectory = clientDirectory;
        _serviceRepository = serviceRepository;

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
            .WithMessage("Lista usług zawiera duplikaty.")
            .MustAsync(BelongToStudioAsync)
            .WithMessage("Wybrana usługa nie istnieje.");


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

        // The directory only sees the current studio, so another studio's client or vehicle is "not found"
        return await _clientDirectory.FindBookingPartiesAsync(dto.ClientId, vehicleId, cancellationToken) != null;
    }

    // The service lookup is tenant-filtered, so IDs of another studio's services don't count.
    private async Task<bool> BelongToStudioAsync(List<Guid> serviceIds, CancellationToken cancellationToken)
    {
        if (serviceIds.Count == 0) return true;

        var distinctIds = serviceIds.Distinct().ToList();

        return await _serviceRepository.CountExistingAsync(distinctIds) == distinctIds.Count;
    }
}