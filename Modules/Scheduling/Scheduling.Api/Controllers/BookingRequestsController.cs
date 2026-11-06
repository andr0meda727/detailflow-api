using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Requests;
using Scheduling.Application.Requests.Accept;
using Scheduling.Application.Requests.GetStudioRequests;
using Scheduling.Application.Requests.Reject;
using Scheduling.Domain.Requests;
using Shared.Api.Errors;
using Shared.Kernel.Pagination;

namespace Scheduling.Api.Controllers;

/// <summary>
/// The current studio's online booking requests, for its staff to answer.
/// </summary>
[ApiController]
[Route("api/booking-requests")]
[Authorize]
public class BookingRequestsController : ControllerBase
{
    /// <summary>
    /// Pending requests first, then the newest; <c>?status=Pending</c> shows only one kind.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<BookingRequestDto>>> GetAll(
        [FromQuery] OnlineRequestStatus? status, [FromQuery] PageRequest paging,
        [FromServices] GetStudioBookingRequestsHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(status, paging, cancellationToken);
    }

    /// <summary>
    /// Creates the booking (at the preferred time and estimated price unless given) and tells the customer.
    /// </summary>
    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<BookingRequestDto>> Accept(
        Guid id, AcceptBookingRequest decision, [FromServices] AcceptBookingRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, decision, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<BookingRequestDto>> Reject(
        Guid id, RejectBookingRequest decision, [FromServices] RejectBookingRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, decision, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }
}
