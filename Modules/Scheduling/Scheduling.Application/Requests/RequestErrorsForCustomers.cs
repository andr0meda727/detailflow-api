using Shared.Kernel.Results;

namespace Scheduling.Application.Requests;

/// <summary>
/// Why a customer's request can't be taken. Kept here, not in the domain: they are about other modules' data.
/// </summary>
public static class CustomerRequestErrors
{
    public static readonly Error StudioNotAvailable =
        Error.NotFound("STUDIO_NOT_FOUND", "To studio nie przyjmuje rezerwacji online.");

    public static readonly Error UnknownServices = Error.Validation(
        "BOOKING_REQUEST_UNKNOWN_SERVICES", "Któraś z wybranych usług nie jest dostępna w tym studiu.");

    public static readonly Error ProfileRequired = Error.Validation(
        "CUSTOMER_PROFILE_REQUIRED", "Uzupełnij imię i numer telefonu w swoim profilu, zanim wyślesz prośbę.");

    public static readonly Error VehicleNotFound =
        Error.NotFound("CUSTOMER_VEHICLE_NOT_FOUND", "Nie znaleziono pojazdu w Twoim garażu.");

    public static readonly Error TooManyPending = Error.Conflict(
        "BOOKING_REQUEST_LIMIT",
        $"Masz już {SubmitLimits.MaxPendingPerStudio} oczekujące prośby w tym studiu. Poczekaj na odpowiedź.");
}

public static class SubmitLimits
{
    /// <summary>
    /// Pending requests one customer may have at one studio, so a studio's queue can't be flooded.
    /// </summary>
    public const int MaxPendingPerStudio = 3;
}
