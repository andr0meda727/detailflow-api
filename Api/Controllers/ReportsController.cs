using Core.DTOs.Reports;
using Core.Messages;
using Core.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController(
    IPublishEndpoint publishEndpoint,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpPost("bookings")]
    public async Task<IActionResult> GenerateBookingsReport(
        [FromBody] BookingsReportRequestDto dto)
    {
        var studioId = tenantContext.StudioId;
        var userId   = tenantContext.UserId;

        if (studioId == null || userId == null)
            return Unauthorized();

        // Pobierz email z claimów — nie chcemy odpytywać DB w kontrolerze
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value;

        if (email == null) return Unauthorized();

        await publishEndpoint.Publish(new GenerateBookingsReportCommand
        {
            StudioId            = studioId.Value,
            RequestedByUserId   = userId.Value,
            RequestedByEmail    = email,
            DateFrom            = dto.DateFrom,
            DateTo              = dto.DateTo
        });

        return Accepted(new { message = "Raport jest generowany. Wyślemy go na Twój email." });
    }
}