using System.Security.Claims;
using Core.Interfaces;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationsController(IInvitationService invitationService, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("{token:guid}")]
    public async Task<IActionResult> GetInvitation(Guid token)
    {
        var invitation = await invitationService.GetByTokenAsync(token);

        if (invitation == null)
            return NotFound(new { error = "Zaproszenie nie istnieje lub wygasło." });

        return Ok(invitation);
    }

    [HttpPost("{token:guid}/accept")]
    [Authorize]
    public async Task<IActionResult> Accept(Guid token)
    {
        var userId = tenantContext.UserId;
        if (userId == null) return Unauthorized();

        var result = await invitationService.AcceptAsync(token, userId.Value);

        if (!result.Success)
            return BadRequest(new { error = result.ErrorCode, message = result.ErrorMessage });

        return Ok(new { message = "Zaproszenie zaakceptowane. Witaj w studiu!" });
    }

    [HttpPost("{token:guid}/decline")]
    [Authorize]
    public async Task<IActionResult> Decline(Guid token)
    {
        var userId = tenantContext.UserId;
        if (userId == null) return Unauthorized();

        var result = await invitationService.DeclineAsync(token, userId.Value);

        if (!result.Success)
            return BadRequest(new { error = result.ErrorCode, message = result.ErrorMessage });

        return Ok(new { message = "Zaproszenie zostało odrzucone." });
    }
}