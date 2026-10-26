using Core.DTOs.Bookings;
using Shared.Kernel.Pagination;

namespace Core.Interfaces.Services;

public interface IBookingService
{
    Task<PagedResponse<BookingResponseDto>> GetAllBookingsAsync(
        PageRequest page,
        bool? isCompleted = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
    Task<BookingResponseDto?> GetBookingByIdAsync(Guid id);

    Task<BookingResponseDto?> AddBookingAsync(BookingCreateDto bookingData);

    Task<BookingResponseDto?> EditBookingAsync(Guid id, BookingCreateDto bookingData);
    Task<bool> DeleteBookingAsync(Guid id);
    Task<BookingResponseDto?> CompleteBookingAsync(Guid id);
    Task<BookingResponseDto?> CancelBookingAsync(Guid id);
}