using Core.DTOs.Bookings;
using Core.Interfaces.Mappings;
using Core.Models;
using Riok.Mapperly.Abstractions;

namespace Core.Mappings;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class BookingMapper : IBookingMapper
{
    [MapProperty("Status.Name", nameof(BookingResponseDto.Status))]
    [MapProperty("Status.Color", nameof(BookingResponseDto.StatusColor))]
    [MapProperty(nameof(Booking.BookingServices), nameof(BookingResponseDto.Services))]
    public partial BookingResponseDto MapBookingToResponse(Booking booking);

    [MapperIgnoreTarget(nameof(Booking.Id))]
    [MapperIgnoreTarget(nameof(Booking.StudioId))]
    [MapperIgnoreTarget(nameof(Booking.CompletedAt))]
    [MapperIgnoreTarget(nameof(Booking.TotalPrice))]
    [MapperIgnoreTarget(nameof(Booking.CreatedBy))]
    [MapperIgnoreTarget(nameof(Booking.CreatedAt))]
    [MapperIgnoreTarget(nameof(Booking.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Booking.Studio))]
    [MapperIgnoreTarget(nameof(Booking.Vehicle))]
    [MapperIgnoreTarget(nameof(Booking.Client))]
    [MapperIgnoreTarget(nameof(Booking.Status))]
    [MapperIgnoreTarget(nameof(Booking.CreatedByUser))]
    [MapperIgnoreTarget(nameof(Booking.BookingServices))]
    public partial Booking MapCreateDtoToBooking(BookingCreateDto dto);

    private static List<string> MapServices(ICollection<BookingServices> services)
    {
        return services
            .Select(s => s.Service?.Name ?? "Unknown service")
            .ToList();
    }
}