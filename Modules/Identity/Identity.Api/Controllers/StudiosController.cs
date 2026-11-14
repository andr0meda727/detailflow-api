using Identity.Application;
using Identity.Application.Studios.ChangeStudioTimeZone;
using Identity.Application.Studios.CreateStudio;
using Identity.Application.Studios.GetMyStudios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Errors;
using Shared.Api.Tenancy;
using Shared.Kernel.Tenancy;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudiosController : ControllerBase
{
    // Used to choose a studio, so it can't require one
    [HttpGet("my")]
    [AllowWithoutStudio]
    public async Task<ActionResult<List<UserStudioDto>>> GetMyStudios(
        [FromServices] ITenantContext tenant, [FromServices] GetMyStudiosHandler handler, CancellationToken cancellationToken)
    {
        if (tenant.UserId is not { } userId) return Unauthorized();

        return await handler.HandleAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Opens a new studio with the caller as its owner. Works without a studio (onboarding right after sign-up) and
    /// for owners adding another location.
    /// </summary>
    [HttpPost]
    [AllowWithoutStudio]
    public async Task<ActionResult<UserStudioDto>> Create(
        CreateStudioRequest request, [FromServices] ITenantContext tenant, [FromServices] CreateStudioHandler handler,
        CancellationToken cancellationToken)
    {
        if (tenant.UserId is not { } userId) return Unauthorized();

        var result = await handler.HandleAsync(userId, request, cancellationToken);

        return result.Success
            ? Created("/api/studios/my", result.Value)
            : this.ToProblem(result.Error!);
    }

    /// <summary>
    /// Sets the current studio's time zone (an IANA name such as "Europe/Warsaw"), used for reports and emails.
    /// </summary>
    [HttpPut("time-zone")]
    [Authorize(Policy = IdentityModule.OwnerOnlyPolicy)]
    public async Task<IActionResult> ChangeTimeZone(
        ChangeStudioTimeZoneRequest request, [FromServices] ChangeStudioTimeZoneHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.Success ? NoContent() : this.ToProblem(result.Error!);
    }
}
