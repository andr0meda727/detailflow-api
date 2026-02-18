using Core.DTOs.Bookings;

namespace Core.Interfaces.Services;

public interface IBookingService 
{
    Task<List<BookingResponseDto>> GetAllBookingsAsync(
        bool? isCompleted = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
    Task<BookingResponseDto?> GetBookingByIdAsync(Guid id);
    
    Task<BookingResponseDto?> AddBooking(BookingCreateDto bookingData);
    
    Task<BookingResponseDto?> EditBooking(Guid id, BookingCreateDto bookingData);
}
