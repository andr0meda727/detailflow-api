using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Planning.Schedule;
using Shared.Api.Authorization;
using Shared.Api.Errors;

namespace Scheduling.Api.Controllers;

/// <summary>
/// The studio's opening hours, closed days and planning rules. Times are the studio's local time.
/// </summary>
[ApiController]
[Route("api/schedule")]
[Authorize]
public class ScheduleController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ScheduleDto>> Get(
        [FromServices] GetScheduleHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(cancellationToken);
    }

    /// <summary>
    /// Replaces the weekly hours and the rules. An empty list of hours switches planning off.
    /// </summary>
    [HttpPut]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<ActionResult<ScheduleDto>> Update(
        ScheduleRequest request, [FromServices] UpdateScheduleHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpPost("closures")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<ActionResult<ScheduleDto>> CloseDay(
        ClosureRequest request, [FromServices] CloseDayHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(request, cancellationToken);
    }

    [HttpDelete("closures/{date}")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<IActionResult> ReopenDay(
        DateOnly date, [FromServices] ReopenDayHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(date, cancellationToken);

        return result.Success ? NoContent() : this.ToProblem(result.Error!);
    }
}
