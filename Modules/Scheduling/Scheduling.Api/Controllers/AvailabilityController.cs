using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Scheduling.Application.Planning.Availability;
using Shared.Api.Errors;
using Shared.Api.RateLimiting;
using Shared.Api.Tenancy;

namespace Scheduling.Api.Controllers;

/// <summary>
/// Free start times for a set of services, e.g. <c>?from=2026-11-16&amp;to=2026-11-20&amp;serviceIds=…&amp;vehicleSize=Large</c>.
/// Days are the studio's local days; times in the response are UTC.
/// </summary>
[ApiController]
[Route("api/availability")]
[Authorize]
public class AvailabilityController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StaffAvailabilityDto>> Get(
        [FromQuery] AvailabilityQuery query, [FromServices] GetStaffAvailabilityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }
}

/// <summary>
/// Free start times on a published studio's page, so customers ask for a time the studio can actually take.
/// </summary>
[ApiController]
[Route("api/public/studios/{studioId:guid}/availability")]
[AllowAnonymous]
[AllowWithoutStudio]
[EnableRateLimiting(RateLimits.Public)]
public class PublicAvailabilityController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PublicAvailabilityDto>> Get(
        Guid studioId, [FromQuery] AvailabilityQuery query, [FromServices] GetPublicAvailabilityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(studioId, query, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }
}
