using Api.Extensions;
using Core.DTOs.Results;
using Core.DTOs.Bookings;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<BookingResponseDto>>> GetAll(
        [FromQuery] bool? isCompleted = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
        {
            return this.ToProblem(Error.Validation("INVALID_DATE_RANGE", "'fromDate' must be less than or equal to 'toDate'."));
        }

        var bookings = await bookingService.GetAllBookingsAsync(isCompleted, fromDate, toDate);
        return Ok(bookings);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> GetById(Guid id)
    {
        var booking = await bookingService.GetBookingByIdAsync(id);

        if (booking == null) return this.ToProblem(Error.NotFound("BOOKING_NOT_FOUND", $"Booking {id} not found"));

        return Ok(booking);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> Create(
        [FromBody] BookingCreateDto bookingData) {
        var addedBooking = await bookingService.AddBookingAsync(bookingData);

        if (addedBooking == null) return this.ToProblem(Error.Validation("BOOKING_NOT_CREATED", "Couldn't add booking."));

        return CreatedAtAction(nameof(GetById), new { id = addedBooking.Id }, addedBooking);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> Update(Guid id, [FromBody] BookingCreateDto bookingData)
    {
        var editedBooking = await bookingService.EditBookingAsync(id, bookingData);

        if (editedBooking == null) return this.ToProblem(Error.NotFound("BOOKING_NOT_FOUND", $"Booking {id} not found"));

        return Ok(editedBooking);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await bookingService.DeleteBookingAsync(id);

        if (!deleted) return this.ToProblem(Error.NotFound("BOOKING_NOT_FOUND", $"Booking {id} not found"));

        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> Complete(Guid id)
    {
        var completedBooking = await bookingService.CompleteBookingAsync(id);

        if (completedBooking == null) return this.ToProblem(Error.NotFound("BOOKING_NOT_FOUND", $"Booking {id} not found"));

        return Ok(completedBooking);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> Cancel(Guid id)
    {
        var cancelledBooking = await bookingService.CancelBookingAsync(id);

        if (cancelledBooking == null) return this.ToProblem(Error.NotFound("BOOKING_NOT_FOUND", $"Booking {id} not found"));

        return Ok(cancelledBooking);
    }
}