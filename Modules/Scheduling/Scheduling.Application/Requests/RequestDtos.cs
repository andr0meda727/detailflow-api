using FluentValidation;
using Scheduling.Domain.Requests;
using Shared.Application.Validation;
using Shared.Kernel.Vehicles;

namespace Scheduling.Application.Requests;

/// <summary>
/// What a customer sends: the studio, services, a car (one from their garage by <see cref="VehicleId"/>, or typed in
/// as <see cref="Vehicle"/>), when they'd like to come (UTC) and optional notes. Their name and phone come from
/// their profile, the email from their account.
/// </summary>
public sealed record SubmitBookingRequest(
    Guid StudioId, List<Guid> ServiceIds, Guid? VehicleId, RequestVehicleDto? Vehicle, DateTime PreferredAt, string? Notes);

public sealed record RequestVehicleDto(string LicensePlate, string? Brand, string? Model, VehicleSize? Size);

public sealed record EstimateRequest(List<Guid> ServiceIds, VehicleSize? VehicleSize);

public sealed record EstimateLineDto(Guid ServiceId, string Name, decimal? Price);

/// <summary>
/// <see cref="Total"/> adds up the priced services; when <see cref="HasUnpricedServices"/> is true some are quoted
/// on request, so the final price will be higher.
/// </summary>
public sealed record EstimateDto(List<EstimateLineDto> Services, decimal Total, bool HasUnpricedServices)
{
    public static EstimateDto From(IReadOnlyCollection<RequestedService> services) => new(
        services.Select(s => new EstimateLineDto(s.ServiceId, s.Name, s.PriceInCents / 100m)).ToList(),
        services.Sum(s => s.PriceInCents ?? 0) / 100m,
        services.Any(s => s.PriceInCents == null));
}

public sealed record RequestVehicleView(string LicensePlate, string? Brand, string? Model, VehicleSize Size);

/// <summary>
/// The booking an accepted request became, as it stands now (it may since have been moved, completed or cancelled).
/// </summary>
public sealed record RequestBookingView(Guid Id, DateTime ScheduledAt, string Status, decimal? TotalPrice);

public sealed record BookingRequestDto(
    Guid Id, Guid StudioId, string StudioName, OnlineRequestStatus Status, DateTime PreferredAt,
    string CustomerName, string CustomerPhone, string CustomerEmail, RequestVehicleView Vehicle,
    List<EstimateLineDto> Services, decimal EstimatedPrice, bool HasUnpricedServices, string? Notes,
    string? RejectionReason, DateTime CreatedAt, DateTime? DecidedAt, RequestBookingView? Booking)
{
    public static BookingRequestDto From(OnlineBookingRequest request, RequestBookingView? booking = null) => new(
        request.Id, request.StudioId, request.StudioName, request.Status, request.PreferredAt,
        request.CustomerName, request.CustomerPhone, request.CustomerEmail,
        new RequestVehicleView(request.VehicleLicensePlate, request.VehicleBrand, request.VehicleModel, request.VehicleSize),
        request.Services.OrderBy(s => s.Name)
            .Select(s => new EstimateLineDto(s.ServiceId, s.Name, s.PriceInCents / 100m)).ToList(),
        request.EstimatedPriceInCents / 100m, request.HasUnpricedServices, request.Notes,
        request.RejectionReason, request.CreatedAt, request.DecidedAt, booking);
}

public class SubmitBookingRequestValidator : AbstractValidator<SubmitBookingRequest>
{
    // Far enough ahead for any studio's calendar, and stops typos like the year 2062
    private static readonly TimeSpan MaxAhead = TimeSpan.FromDays(366);

    public SubmitBookingRequestValidator(TimeProvider clock)
    {
        RuleFor(x => x.StudioId).NotEmpty().WithMessage("Wybierz studio.");

        RuleFor(x => x.ServiceIds)
            .NotEmpty().WithMessage("Wybierz przynajmniej jedną usługę.")
            .Must(ids => ids.Count <= 20).WithMessage("Można wybrać najwyżej 20 usług.");

        RuleFor(x => x)
            .Must(x => (x.VehicleId == null) != (x.Vehicle == null))
            .WithName("Vehicle")
            .WithMessage("Wybierz pojazd z garażu albo wpisz jego dane.");

        When(x => x.Vehicle != null, () =>
        {
            RuleFor(x => x.Vehicle!.LicensePlate).ValidLicensePlate();
            RuleFor(x => x.Vehicle!.Brand).ValidBrandOrModel("Marka");
            RuleFor(x => x.Vehicle!.Model).ValidBrandOrModel("Model");
            RuleFor(x => x.Vehicle!.Size).NotNull().WithMessage("Wybierz rozmiar pojazdu.").ValidVehicleSize();
        });

        RuleFor(x => x.PreferredAt)
            .Must(at => at > clock.GetUtcNow().UtcDateTime)
            .WithMessage("Termin musi być w przyszłości.")
            .Must(at => at <= clock.GetUtcNow().UtcDateTime.Add(MaxAhead))
            .WithMessage("Termin może być najwyżej rok naprzód.");

        RuleFor(x => x.Notes)
            .MaximumLength(OnlineBookingRequest.MaxNotesLength)
            .WithMessage($"Uwagi mogą mieć najwyżej {OnlineBookingRequest.MaxNotesLength} znaków.");
    }
}

public class EstimateRequestValidator : AbstractValidator<EstimateRequest>
{
    public EstimateRequestValidator()
    {
        RuleFor(x => x.ServiceIds)
            .NotEmpty().WithMessage("Wybierz przynajmniej jedną usługę.")
            .Must(ids => ids.Count <= 20).WithMessage("Można wybrać najwyżej 20 usług.");
        RuleFor(x => x.VehicleSize).NotNull().WithMessage("Wybierz rozmiar pojazdu.").ValidVehicleSize();
    }
}
