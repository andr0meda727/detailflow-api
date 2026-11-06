using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Scheduling.Application.Requests;
using Scheduling.Application.Requests.GetMyRequests;
using Scheduling.Application.Requests.Submit;
using Scheduling.Application.Requests.Withdraw;
using Shared.Api.Errors;
using Shared.Api.RateLimiting;
using Shared.Api.Tenancy;
using Shared.Kernel.Pagination;

namespace Scheduling.Api.Controllers;

/// <summary>
/// A signed-in customer's booking requests at any studio, and the bookings they became.
/// </summary>
[ApiController]
[Route("api/me/booking-requests")]
[Authorize]
[AllowWithoutStudio]
public class MyBookingRequestsController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimits.Public)]
    public async Task<ActionResult<BookingRequestDto>> Submit(
        SubmitBookingRequest request, [FromServices] SubmitBookingRequestHandler handler, CancellationToken cancellationToken)
    {
        // Supabase puts the account's email in the token; the studio replies to it
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
        if (email == null) return Unauthorized();

        var result = await handler.HandleAsync(request, email, cancellationToken);
        if (!result.Success) return this.ToProblem(result.Error!);

        return Created($"/api/me/booking-requests/{result.Value!.Id}", result.Value);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<BookingRequestDto>>> GetMine(
        [FromQuery] PageRequest paging, [FromServices] GetMyBookingRequestsHandler handler,
        CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(paging, cancellationToken);
    }

    [HttpPost("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(
        Guid id, [FromServices] WithdrawBookingRequestHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? NoContent() : this.ToProblem(result.Error!);
    }
}
