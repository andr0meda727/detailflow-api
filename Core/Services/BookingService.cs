using Core.Constants;
using Core.DTOs.Bookings;
using Shared.Kernel.Tenancy;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;
using Crm.Contracts;
using Shared.Kernel.Pagination;

namespace Core.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    ITenantContext tenantContext,
    IBookingMapper mapper,
    IClientDirectory clientDirectory)
    : IBookingService
{
    public async Task<PagedResponse<BookingResponseDto>> GetAllBookingsAsync(
        PageRequest page,
        bool? isCompleted = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var bookings = await bookingRepository.GetAllWithDetailsAsync(page, isCompleted, fromDate, toDate);

        return bookings.Map(mapper.MapBookingToResponse);
    }

    public async Task<BookingResponseDto?> GetBookingByIdAsync(Guid id)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(id);
        return booking == null ? null : mapper.MapBookingToResponse(booking);
    }

    public async Task<BookingResponseDto?> AddBookingAsync(BookingCreateDto bookingData)
    {
        var parties = await clientDirectory.FindBookingPartiesAsync(bookingData.ClientId, bookingData.VehicleId);
        if (parties == null) return null;

        var booking = mapper.MapCreateDtoToBooking(bookingData);
        CopyParties(parties, booking);

        booking.CreatedBy = tenantContext.UserId;
        booking.TotalPrice = (long)Math.Round(bookingData.Price * 100);

        if (bookingData.ServiceIds.Any())
        {
            booking.BookingServices = bookingData.ServiceIds.Select(serviceId => new BookingServices
            {
                ServiceId = serviceId,
            }).ToList();
        }

        var addedBooking = await bookingRepository.AddAsync(booking);

        var bookingWithDetails = await bookingRepository.GetByIdWithDetailsAsync(addedBooking.Id);

        return bookingWithDetails == null ? null : mapper.MapBookingToResponse(bookingWithDetails);
    }

    public async Task<BookingResponseDto?> EditBookingAsync(Guid id, BookingCreateDto bookingData)
    {
        var existingBooking = await bookingRepository.GetByIdWithDetailsAsync(id, trackChanges: true);
        if (existingBooking == null) return null;

        var parties = await clientDirectory.FindBookingPartiesAsync(bookingData.ClientId, bookingData.VehicleId);
        if (parties == null) return null;

        mapper.UpdateBooking(bookingData, existingBooking);
        CopyParties(parties, existingBooking);

        existingBooking.TotalPrice = (long)Math.Round(bookingData.Price * 100);

        existingBooking.BookingServices.Clear();

        foreach (var serviceId in bookingData.ServiceIds)
        {
            existingBooking.BookingServices.Add(new BookingServices
            {
                ServiceId = serviceId,
            });
        }

        await bookingRepository.UpdateAsync(existingBooking);
        var withDetails = await bookingRepository.GetByIdWithDetailsAsync(id);
        return withDetails == null ? null : mapper.MapBookingToResponse(withDetails);
    }

    public async Task<bool> DeleteBookingAsync(Guid id)
    {
        var booking = await bookingRepository.GetByIdAsync(id);

        if (booking == null) return false;

        await bookingRepository.DeleteAsync(id);

        return true;
    }

    public async Task<BookingResponseDto?> CompleteBookingAsync(Guid id)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(id, trackChanges: true);
        if (booking == null) return null;

        booking.StatusId = BookingStatusIds.Completed;
        booking.CompletedAt = DateTime.UtcNow;

        await bookingRepository.UpdateAsync(booking);
        var updatedBooking = await bookingRepository.GetByIdWithDetailsAsync(id);
        return updatedBooking == null ? null : mapper.MapBookingToResponse(updatedBooking);
    }

    public async Task<BookingResponseDto?> CancelBookingAsync(Guid id)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(id, trackChanges: true);

        if (booking == null) return null;

        booking.StatusId = BookingStatusIds.Cancelled;
        booking.CompletedAt = DateTime.UtcNow;

        await bookingRepository.UpdateAsync(booking);

        var updatedBooking = await bookingRepository.GetByIdWithDetailsAsync(id);
        return updatedBooking == null ? null : mapper.MapBookingToResponse(updatedBooking);
    }

    private static void CopyParties(BookingParties parties, Booking booking)
    {
        booking.ClientName = parties.Client.Name;
        booking.ClientPhone = parties.Client.Phone;
        booking.ClientEmail = parties.Client.Email;
        booking.VehicleLicensePlate = parties.Vehicle.LicensePlate;
        booking.VehicleBrand = parties.Vehicle.Brand;
        booking.VehicleModel = parties.Vehicle.Model;
    }
}
