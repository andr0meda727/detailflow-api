using Identity.Application;
using Identity.Application.Invitations.AcceptInvitation;
using Identity.Application.Invitations.DeclineInvitation;
using Identity.Application.Invitations.GetInvitation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Errors;
using Shared.Api.Tenancy;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Identity.Api.Controllers;

/// <summary>
/// Answering an invitation happens before the person works in the studio, so no studio is required here.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowWithoutStudio]
public class InvitationsController : ControllerBase
{
    // Public: the link in the email opens this before the person signs in
    [HttpGet("{token:guid}")]
    public async Task<ActionResult<InvitationResponseDto>> GetInvitation(
        Guid token, [FromServices] GetInvitationHandler handler, CancellationToken cancellationToken)
    {
        var invitation = await handler.HandleAsync(token, cancellationToken);

        return invitation != null
            ? invitation
            : this.ToProblem(Error.NotFound("INVITATION_NOT_FOUND", "Zaproszenie nie istnieje lub wygasło."));
    }

    [HttpPost("{token:guid}/accept")]
    [Authorize]
    public async Task<IActionResult> Accept(
        Guid token, [FromServices] ITenantContext tenant, [FromServices] AcceptInvitationHandler handler,
        CancellationToken cancellationToken)
    {
        if (tenant.UserId is not { } userId) return Unauthorized();

        var result = await handler.HandleAsync(token, userId, cancellationToken);

        return result.Success
            ? Ok(new { message = "Zaproszenie zaakceptowane. Witaj w studiu!" })
            : this.ToProblem(result.Error!);
    }

    [HttpPost("{token:guid}/decline")]
    [Authorize]
    public async Task<IActionResult> Decline(
        Guid token, [FromServices] ITenantContext tenant, [FromServices] DeclineInvitationHandler handler,
        CancellationToken cancellationToken)
    {
        if (tenant.UserId is not { } userId) return Unauthorized();

        var result = await handler.HandleAsync(token, userId, cancellationToken);

        return result.Success
            ? Ok(new { message = "Zaproszenie zostało odrzucone." })
            : this.ToProblem(result.Error!);
    }
}
