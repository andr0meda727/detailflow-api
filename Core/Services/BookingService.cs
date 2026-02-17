using Core.DTOs.Bookings;
using Core.Interfaces;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Models;

namespace Core.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IBookingServicesRepository bookingServicesRepository,
    ITenantContext tenantContext,
    IBookingMapper mapper)
    : IBookingService
{
    public async Task<List<BookingResponseDto>> GetAllBookingsAsync(
        bool? isCompleted = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var bookings = await bookingRepository.GetAllWithDetailsAsync(isCompleted, fromDate, toDate);
        return bookings.Select(mapper.MapBookingToResponse).ToList();
    }

    public async Task<BookingResponseDto?> GetBookingByIdAsync(Guid id)
    {
        var booking = await bookingRepository.GetByIdAsync(id);
        return booking != null ? mapper.MapBookingToResponse(booking) : new BookingResponseDto();
    }

    public async Task<BookingResponseDto?> AddBooking(BookingCreateDto bookingData)
    {
        var now = DateTime.UtcNow;
        
        var bookingGuid = Guid.NewGuid();

        var booking = new Booking
        {
            Id = bookingGuid,
            StudioId = tenantContext.StudioId,
            VehicleId = bookingData.VehicleId,
            ClientId = bookingData.ClientId,
            StatusId = bookingData.StatusId,
            ScheduledAt = bookingData.ScheduledAt,
            Notes = bookingData.Notes,
            CreatedBy = tenantContext.UserId,
            CreatedAt = now,
            UpdatedAt = now,

            // adjust this conversion to your money format
            TotalPrice = (long)Math.Round(bookingData.Price),

            BookingServices = bookingData.ServiceIds.Select(serviceId => new BookingServices
            {
                BookingId = bookingGuid,
                ServiceId = serviceId,
                CreatedAt = now
            }).ToList()
        };

        var addedBooking = await bookingRepository.AddAsync(booking);
        return mapper.MapBookingToResponse(addedBooking);
    }

    public async Task<BookingResponseDto?> EditBooking(Guid id, BookingCreateDto bookingData)
    {
        var now = DateTime.UtcNow;

        var booking = new Booking
        {
            Id = id,
            StudioId = tenantContext.StudioId,
            VehicleId = bookingData.VehicleId,
            ClientId = bookingData.ClientId,
            StatusId = bookingData.StatusId,
            ScheduledAt = bookingData.ScheduledAt,
            Notes = bookingData.Notes,
            UpdatedAt = now,

            // adjust this conversion to your money format
            TotalPrice = (long)Math.Round(bookingData.Price),

            BookingServices = bookingData.ServiceIds.Select(serviceId => new BookingServices
            {
                BookingId = id,
                ServiceId = serviceId,
                CreatedAt = now
            }).ToList()
        };

        await bookingRepository.UpdateAsync(booking);
        return mapper.MapBookingToResponse(booking);
    }
}
