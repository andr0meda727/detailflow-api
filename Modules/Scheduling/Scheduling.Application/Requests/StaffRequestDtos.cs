using FluentValidation;

namespace Scheduling.Application.Requests;

/// <summary>
/// Accepting a request, optionally at another time or a final price (złoty) than the customer's preferred time and
/// the estimate, and on a chosen bay (left out, the best-fitting free one).
/// </summary>
public sealed record AcceptBookingRequest(DateTime? ScheduledAt = null, decimal? Price = null, Guid? BayId = null);

public sealed record RejectBookingRequest(string? Reason = null);

public class AcceptBookingRequestValidator : AbstractValidator<AcceptBookingRequest>
{
    public AcceptBookingRequestValidator(TimeProvider clock)
    {
        RuleFor(x => x.ScheduledAt!.Value)
            .Must(at => at > clock.GetUtcNow().UtcDateTime.AddMinutes(-10))
            .WithName("ScheduledAt")
            .WithMessage("Termin nie może być w przeszłości.")
            .When(x => x.ScheduledAt.HasValue);

        RuleFor(x => x.Price!.Value).ValidPrice().WithName("Price").When(x => x.Price.HasValue);
    }
}

public class RejectBookingRequestValidator : AbstractValidator<RejectBookingRequest>
{
    public RejectBookingRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage("Powód może mieć najwyżej 500 znaków.");
    }
}
