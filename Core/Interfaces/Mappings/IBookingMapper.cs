using Core.DTOs.Bookings;
using Core.Models;

namespace Core.Interfaces.Mappings;

public interface IBookingMapper
{
    public BookingResponseDto MapBookingToResponse(Booking booking);
}