using Core.DTOs.Bookings;
using Core.Interfaces.Mappings;
using Core.Models;
using Riok.Mapperly.Abstractions;

namespace Core.Mappings;

[Mapper]
public partial class BookingMapper : IBookingMapper
{
    public partial BookingResponseDto MapBookingToResponse(Booking booking);
}