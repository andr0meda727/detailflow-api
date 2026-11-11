using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Bookings;
using Scheduling.Application.Bookings.CancelBooking;
using Scheduling.Application.Bookings.CompleteBooking;
using Scheduling.Application.Bookings.CreateBooking;
using Scheduling.Application.Bookings.DeleteBooking;
using Scheduling.Application.Bookings.GetBooking;
using Scheduling.Application.Bookings.GetBookings;
using Scheduling.Application.Bookings.UpdateBooking;
using Shared.Api.Errors;
using Shared.Kernel.Pagination;

namespace Scheduling.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<BookingResponseDto>>> GetAll(
        [FromQuery] PageRequest paging, [FromQuery] bool? isCompleted, [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate, [FromServices] GetBookingsHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(paging, isCompleted, fromDate, toDate, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponseDto>> GetById(
        Guid id, [FromServices] GetBookingHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>> Create(
        BookingRequest request, [FromServices] CreateBookingHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : this.ToProblem(result.Error!);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BookingResponseDto>> Update(
        Guid id, BookingRequest request, [FromServices] UpdateBookingHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id, [FromServices] DeleteBookingHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? NoContent() : this.ToProblem(result.Error!);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<BookingResponseDto>> Complete(
        Guid id, [FromServices] CompleteBookingHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<BookingResponseDto>> Cancel(
        Guid id, [FromServices] CancelBookingHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }
}
