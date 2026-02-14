using Core.DTOs.Bookings;
using Core.Interfaces;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;

namespace Core.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    ITenantContext tenantContext,
    IBookingMapper mapper)
    : IBookingService
{
    public async Task<List<BookingResponseDto>> GetAllBookingsAsync()
    {
        var bookings = await bookingRepository.GetAllWithDetailsAsync();
        return bookings.Select(mapper.MapBookingToResponse).ToList();
    }
}
