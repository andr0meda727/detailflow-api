using Core.DTOs.Bookings;
using Core.Interfaces;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BookingResponseDto>>> GetAllBookings()
    {
        var bookings = await bookingService.GetAllBookingsAsync();
        return Ok(bookings);
    }
}