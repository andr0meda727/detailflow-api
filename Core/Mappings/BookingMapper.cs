using Core.DTOs.Bookings;
using Core.Interfaces.Mappings;
using Core.Models;
using Riok.Mapperly.Abstractions;

namespace Core.Mappings;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class BookingMapper : IBookingMapper
{
    [MapProperty(nameof(Booking.Status.Name), nameof(BookingResponseDto.Status))]
    [MapProperty(nameof(Booking.Status.Color), nameof(BookingResponseDto.StatusColor))]
    [MapProperty(nameof(Booking.BookingServices), nameof(BookingResponseDto.Services))]
    public partial BookingResponseDto MapBookingToResponse(Booking booking);

    private List<string> MapServices(ICollection<BookingService> services)
    {
        return services
            .Select(s => s.Service?.Name ?? "Unknown service")
            .ToList();
    }
}