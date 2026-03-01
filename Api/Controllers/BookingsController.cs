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
            return BadRequest("'fromDate' must be less than or equal to 'toDate'.");
        }

        var bookings = await bookingService.GetAllBookingsAsync(isCompleted, fromDate, toDate);
        return Ok(bookings);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> GetById(Guid id)
    {
        var bookings = await bookingService.GetBookingByIdAsync(id);
        return Ok(bookings);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> Create(
        [FromBody] BookingCreateDto bookingData) {
        var addedBooking = await bookingService.AddBookingAsync(bookingData);

        if (addedBooking == null) return BadRequest("Couldn't add booking.");

        return CreatedAtAction(nameof(GetById), new { id = addedBooking.Id }, addedBooking);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> Update(Guid id, [FromBody] BookingCreateDto bookingData)
    {
        var editedBooking = await bookingService.EditBookingAsync(id, bookingData);

        if (editedBooking == null) return NotFound("Booking not found.");

        return Ok(editedBooking);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await bookingService.DeleteBookingAsync(id);

        if (!deleted) return NotFound($"Booking {id} doesn't exist");

        return NoContent();
    }
}