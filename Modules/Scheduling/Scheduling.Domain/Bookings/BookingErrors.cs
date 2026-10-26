using Shared.Kernel.Results;

namespace Scheduling.Domain.Bookings;

public static class BookingErrors
{
    public static readonly Error AlreadyFinished = Error.Conflict(
        "BOOKING_ALREADY_FINISHED", "The booking is already completed or cancelled and can no longer change.");
}
