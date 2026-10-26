using Shared.Kernel.Results;

namespace Scheduling.Application;

public static class SchedulingErrors
{
    public static Error BookingNotFound(Guid id) =>
        Error.NotFound("BOOKING_NOT_FOUND", $"Booking {id} not found");

    public static Error ServiceNotFound(Guid id) =>
        Error.NotFound("SERVICE_NOT_FOUND", $"Service {id} not found");

    // The validator checks this first; this covers the client or vehicle disappearing in between
    public static readonly Error UnknownClientOrVehicle = Error.Validation(
        "BOOKING_CLIENT_OR_VEHICLE_NOT_FOUND", "The client or vehicle doesn't exist, or the vehicle isn't the client's.");

    public static readonly Error InvalidDateRange = Error.Validation(
        "INVALID_DATE_RANGE", "'fromDate' must be less than or equal to 'toDate'.");
}
