using Identity.Application;
using Identity.Application.Studios.GetMyStudios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
