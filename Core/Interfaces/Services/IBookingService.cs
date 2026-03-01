using Core.DTOs.Bookings;

namespace Core.Interfaces.Services;

public interface IBookingService
{
    Task<List<BookingResponseDto>> GetAllBookingsAsync(
        bool? isCompleted = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
    Task<BookingResponseDto?> GetBookingByIdAsync(Guid id);

    Task<BookingResponseDto?> AddBookingAsync(BookingCreateDto bookingData);

    Task<BookingResponseDto?> EditBookingAsync(Guid id, BookingCreateDto bookingData);
    Task<bool> DeleteBookingAsync(Guid id);
}