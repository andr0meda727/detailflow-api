using System.Security.Claims;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Contracts;
using Shared.Api.Errors;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Notifications.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    /// <summary>
    /// Queues a PDF of the studio's bookings in the period; it arrives by email to the signed-in user.
    /// </summary>
    [HttpPost("bookings")]
    public async Task<IActionResult> GenerateBookingsReport(
        BookingsReportRequest request, [FromServices] ITenantContext tenant, [FromServices] IPublishEndpoint bus,
        CancellationToken cancellationToken)
    {
        if (tenant.StudioId is not { } studioId)
        {
            return this.ToProblem(Error.Forbidden("STUDIO_REQUIRED", "A report needs a studio."));
        }

        // Supabase puts the account's email in the token, so no lookup is needed
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
        if (email == null) return Unauthorized();

        await bus.Publish(new GenerateBookingsReport(studioId, email, request.DateFrom, request.DateTo), cancellationToken);

        return Accepted(new { message = "Raport jest generowany. Wyślemy go na Twój email." });
    }
}

public sealed record BookingsReportRequest(DateTime DateFrom, DateTime DateTo);

public class BookingsReportRequestValidator : AbstractValidator<BookingsReportRequest>
{
    public BookingsReportRequestValidator()
    {
        RuleFor(x => x.DateFrom).NotEmpty().WithMessage("Data początkowa jest wymagana.");
        RuleFor(x => x.DateTo)
            .NotEmpty().WithMessage("Data końcowa jest wymagana.")
            .GreaterThanOrEqualTo(x => x.DateFrom).WithMessage("Data końcowa nie może być wcześniejsza niż początkowa.")
            .Must((request, to) => to - request.DateFrom <= TimeSpan.FromDays(366))
            .WithMessage("Raport może obejmować najwyżej rok.");
    }
}
