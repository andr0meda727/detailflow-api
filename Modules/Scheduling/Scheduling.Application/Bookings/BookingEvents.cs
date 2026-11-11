using Scheduling.Application.Abstractions;
using Scheduling.Contracts;
using Scheduling.Domain.Bookings;

namespace Scheduling.Application.Bookings;

internal static class BookingEvents
{
    /// <summary>
    /// Queues <see cref="BookingCompleted"/> for a booking that was just completed, saved with the same SaveChanges.
    /// Call it wherever a booking can become completed: the complete endpoint, an edit or a new booking entered as done.
    /// The studio is passed in because a new booking only gets its own on save. Its service lines must be loaded.
    /// </summary>
    public static void EnqueueIfCompleted(this ISchedulingDbContext db, Booking booking, Guid studioId, bool wasCompleted)
    {
        if (wasCompleted || booking.StatusId != BookingStatus.Completed) return;

        db.Enqueue(new BookingCompleted(
            studioId, booking.Id, booking.CustomerUserId, booking.VehicleSize.ToString(),
            booking.Services.Select(s => s.ServiceId).ToList(), booking.CompletedAt ?? DateTime.UtcNow));
    }
}
