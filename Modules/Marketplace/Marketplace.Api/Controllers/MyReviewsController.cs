using Marketplace.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Api.Errors;
using Shared.Api.RateLimiting;
using Shared.Api.Tenancy;

namespace Marketplace.Api.Controllers;

/// <summary>
/// The signed-in customer's reviews of their completed visits. Needs no studio.
/// </summary>
[ApiController]
[Route("api/me/reviews")]
[Authorize]
[AllowWithoutStudio]
public class MyReviewsController : ControllerBase
{
    /// <summary>
    /// Completed visits the customer can still review (up to 90 days after the visit).
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<List<ReviewableVisitDto>>> Pending(
        [FromServices] GetReviewableVisitsHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(cancellationToken);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimits.Public)]
    public async Task<ActionResult<ReviewDto>> Submit(
        SubmitReviewRequest request, [FromServices] SubmitReviewHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.Success
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : this.ToProblem(result.Error!);
    }
}
