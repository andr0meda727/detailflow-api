using Core.DTOs.Bookings;

namespace Core.Interfaces.Services;

public interface IBookingService
{
    Task<List<BookingResponseDto>> GetAllBookingsAsync();
}