using Crm.Contracts;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Bookings;

namespace Scheduling.Application.Bookings;

public class BookingRequestValidator : AbstractValidator<BookingRequest>
{
    // Bookings cannot be scheduled further than 2 years into the future
    private static readonly TimeSpan MaxFutureBooking = TimeSpan.FromDays(365 * 2);

    private readonly IClientDirectory _clientDirectory;
    private readonly ISchedulingDbContext _db;

    public BookingRequestValidator(IClientDirectory clientDirectory, ISchedulingDbContext db, TimeProvider clock)
    {
        _clientDirectory = clientDirectory;
        _db = db;

        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("ID klienta jest wymagane.");

        RuleFor(x => x.StatusId)
            .NotEmpty().WithMessage("Musisz wybrać status rezerwacji.")
            .Must(BookingStatus.IsKnown).WithMessage("Wybrany status rezerwacji nie istnieje.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notatki są za długie (maksymalnie 2000 znaków).")
            .Must(n => n!.Trim().Length > 0)
            .WithMessage("Notatki nie mogą składać się tylko z białych znaków.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Price).ValidPrice();

        RuleFor(x => x.ServiceIds)
            .NotEmpty().WithMessage("Musisz wybrać przynajmniej jedną usługę.")
            .Must(ids => ids.Count <= 30).WithMessage("Możesz wybrać maksymalnie 30 usług na raz.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Lista usług zawiera duplikaty.")
            .MustAsync(BeActiveServicesOfStudioAsync)
            .WithMessage("Wybrana usługa nie istnieje.");

        RuleFor(x => x.ScheduledAt)
            .NotEmpty().WithMessage("Data planowanej rezerwacji jest wymagana.")
            .Must(at => at > clock.GetUtcNow().UtcDateTime.AddMinutes(-10))
            .WithMessage("Data rezerwacji nie może być w przeszłości.")
            .Must(at => at <= clock.GetUtcNow().UtcDateTime.Add(MaxFutureBooking))
            .WithMessage("Rezerwacja nie może być zaplanowana na więcej niż 2 lata do przodu.");

        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Musisz podać ID pojazdu.")
            .MustAsync(BelongToClientAsync)
            .WithMessage("Wybrany pojazd nie istnieje lub nie należy do podanego klienta.");
    }

    // The directory only sees the current studio, so another studio's client or vehicle is "not found"
    private async Task<bool> BelongToClientAsync(BookingRequest request, Guid vehicleId, CancellationToken cancellationToken)
    {
        if (request.ClientId == Guid.Empty || vehicleId == Guid.Empty) return false;

        return await _clientDirectory.FindBookingPartiesAsync(request.ClientId, vehicleId, cancellationToken) != null;
    }

    // Services are tenant-filtered, so IDs of another studio's services don't count
    private async Task<bool> BeActiveServicesOfStudioAsync(List<Guid> serviceIds, CancellationToken cancellationToken)
    {
        if (serviceIds.Count == 0) return true;

        var distinct = serviceIds.Distinct().ToList();
        var found = await _db.Services.CountAsync(s => distinct.Contains(s.Id) && s.IsActive, cancellationToken);

        return found == distinct.Count;
    }
}
