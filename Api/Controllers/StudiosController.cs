using Core.Interfaces;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudiosController(IStudioService studioService, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("my")]
    public async Task<IActionResult> GetMyStudios()
    {
        var userId = tenantContext.UserId;
        if (userId == null) return Unauthorized();

        var studios = await studioService.GetStudiosForUserAsync(userId.Value);
        return Ok(studios);
    }
}