using Core.Interfaces;
using Core.Interfaces.Services;
using Core.DTOs.Studio;
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

    [HttpPost]
    public async Task<ActionResult<UserStudioDto>> Create([FromBody] CreateStudioDto dto)
    {
        var userId = tenantContext.UserId;
        if (userId == null) return Unauthorized();

        var studio = await studioService.CreateForUserAsync(userId.Value, dto);
        return CreatedAtAction(nameof(GetMyStudios), studio);
    }

    [HttpPut("{studioId:guid}")]
    public async Task<ActionResult<UserStudioDto>> Update(Guid studioId, [FromBody] UpdateStudioDto dto)
    {
        var userId = tenantContext.UserId;
        if (userId == null) return Unauthorized();

        var studio = await studioService.UpdateAsync(studioId, userId.Value, dto);
        if (studio == null) return NotFound(new { error = "NOT_FOUND", message = "Studio nie istnieje lub nie masz uprawnień do jego edycji." });

        return Ok(studio);
    }
}
