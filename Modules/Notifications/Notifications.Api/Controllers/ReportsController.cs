using System.Security.Claims;
using FluentValidation;
using Identity.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Contracts;
using Shared.Api.Errors;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;
using Shared.Kernel.Time;

namespace Notifications.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    /// <summary>
    /// Queues a PDF of the studio's bookings on the given days (in the studio's time zone, both included);
    /// it arrives by email to the signed-in user.
    /// </summary>
    [HttpPost("bookings")]
    public async Task<IActionResult> GenerateBookingsReport(
        BookingsReportRequest request, [FromServices] ITenantContext tenant, [FromServices] IStudioDirectory studios,
        [FromServices] IPublishEndpoint bus, CancellationToken cancellationToken)
    {
        if (tenant.StudioId is not { } studioId)
        {
            return this.ToProblem(Error.Forbidden("STUDIO_REQUIRED", "A report needs a studio."));
        }

        // Supabase puts the account's email in the token, so no lookup is needed
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
        if (email == null) return Unauthorized();

        var timeZone = await studios.FindTimeZoneAsync(studioId, cancellationToken) ?? TimeZones.Default;

        await bus.Publish(
            new GenerateBookingsReport(studioId, email, request.DateFrom, request.DateTo, timeZone), cancellationToken);

        return Accepted(new { message = "Raport jest generowany. Wyślemy go na Twój email." });
    }
}

/// <summary>
/// Calendar days in the studio's time zone, sent as "yyyy-MM-dd".
/// </summary>
public sealed record BookingsReportRequest(DateOnly DateFrom, DateOnly DateTo);

public class BookingsReportRequestValidator : AbstractValidator<BookingsReportRequest>
{
    // A whole leap year is 366 days: from the 1st day to the 366th is 365 days apart
    private const int MaxDaysApart = 365;

    public BookingsReportRequestValidator()
    {
        RuleFor(x => x.DateFrom).NotEmpty().WithMessage("Data początkowa jest wymagana.");
        RuleFor(x => x.DateTo)
            .NotEmpty().WithMessage("Data końcowa jest wymagana.")
            .GreaterThanOrEqualTo(x => x.DateFrom).WithMessage("Data końcowa nie może być wcześniejsza niż początkowa.")
            .Must((request, to) => to.DayNumber - request.DateFrom.DayNumber <= MaxDaysApart)
            .WithMessage("Raport może obejmować najwyżej rok.");
    }
}
