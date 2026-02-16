using Core.DTOs.Bookings;
using Core.Models;

namespace Core.Interfaces.Mappings;

public interface IBookingMapper
{
    BookingResponseDto MapBookingToResponse(Booking booking);
}