using Core.DTOs.Bookings;
using Core.Interfaces;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Core.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IBookingMapper mapper)
    : IBookingService
{
    public async Task<List<BookingResponseDto>> GetAllBookingsAsync()
    {
        var bookings = await bookingRepository.GetAllWithDetailsAsync();
        return bookings.Select(mapper.MapBookingToResponse).ToList();
    }
}