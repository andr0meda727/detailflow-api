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
    public async Task<ActionResult<List<BookingResponseDto>>> GetAllBookings()
    {
        var bookings = await bookingService.GetAllBookingsAsync();
        return Ok(bookings);
    }

    [HttpGet("{id:guid}")]

    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> GetBookingById(Guid id)
    {
        var bookings = await bookingService.GetBookingByIdAsync(id);
        return Ok(bookings);
    }
}