using Core.DTOs.Bookings;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<BookingResponseDto>>> GetAllBookings(
        [FromQuery] bool? isCompleted = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
        {
            return BadRequest("'fromDate' must be less than or equal to 'toDate'.");
        }

        var bookings = await bookingService.GetAllBookingsAsync(isCompleted, fromDate, toDate);
        return Ok(bookings);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> GetBookingById(Guid id)
    {
        var bookings = await bookingService.GetBookingByIdAsync(id);
        return Ok(bookings);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking(BookingCreateDto bookingData)
    {
        var addedBooking = await bookingService.AddBooking(bookingData);
        return Ok(addedBooking);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> UpdateBooking(Guid id, BookingCreateDto bookingData)
    {
        var editedBooking = await bookingService.EditBooking(id, bookingData);
        return Ok(editedBooking);
    }
}
