using Core.DTOs.Bookings;

namespace Core.Interfaces;

public interface IBookingService 
{
    Task<List<BookingResponseDto>> GetAllBookingsAsync();
    Task<BookingResponseDto?> GetBookingByIdAsync(Guid id);
}