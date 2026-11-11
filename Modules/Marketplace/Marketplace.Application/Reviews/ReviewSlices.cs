using Customers.Contracts;
using FluentValidation;
using Marketplace.Application.Abstractions;
using Marketplace.Domain;
using Marketplace.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Pagination;
using Shared.Kernel.Pagination;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Marketplace.Application.Reviews;

public sealed record SubmitReviewRequest(Guid BookingId, int Rating, string? Comment);

public sealed record ReviewDto(Guid Id, string AuthorName, int Rating, string? Comment, DateTime CreatedAt)
{
    public static ReviewDto From(Review r) => new(r.Id, r.AuthorName, r.Rating, r.Comment, r.CreatedAt);
}

/// <summary>
/// A completed visit the customer can still review, until <see cref="ReviewableUntil"/>.
/// </summary>
public sealed record ReviewableVisitDto(Guid BookingId, Guid StudioId, string? StudioName, DateTime CompletedAt, DateTime ReviewableUntil);

/// <summary>
/// A studio's rating: the average of its reviews (one decimal), null before the first one.
/// </summary>
public sealed record RatingSummary(double? AverageRating, int ReviewCount)
{
    public static readonly RatingSummary None = new(null, 0);
}

public class SubmitReviewRequestValidator : AbstractValidator<SubmitReviewRequest>
{
    public SubmitReviewRequestValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty().WithMessage("Podaj wizytę.");
        RuleFor(x => x.Rating).InclusiveBetween(Review.MinRating, Review.MaxRating)
            .WithMessage($"Ocena musi wynosić od {Review.MinRating} do {Review.MaxRating}.");
        RuleFor(x => x.Comment).MaximumLength(Review.MaxCommentLength)
            .WithMessage($"Komentarz może mieć najwyżej {Review.MaxCommentLength} znaków.");
    }
}

public static class RatingQueries
{
    /// <summary>
    /// The rating of each of these studios, worked out from their reviews (indexed by studio), so it is always
    /// exactly what the reviews say.
    /// </summary>
    public static async Task<Dictionary<Guid, RatingSummary>> RatingsAsync(
        this IMarketplaceDbContext db, IReadOnlyCollection<Guid> studioIds, CancellationToken cancellationToken)
    {
        var rows = await db.Reviews.AsNoTracking()
            .Where(r => studioIds.Contains(r.StudioId))
            .GroupBy(r => r.StudioId)
            .Select(g => new { StudioId = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.StudioId, r => new RatingSummary(Math.Round(r.Average, 1), r.Count));
    }

    public static async Task<RatingSummary> RatingAsync(
        this IMarketplaceDbContext db, Guid studioId, CancellationToken cancellationToken) =>
        (await db.RatingsAsync([studioId], cancellationToken)).GetValueOrDefault(studioId) ?? RatingSummary.None;
}

public sealed class SubmitReviewHandler(
    IMarketplaceDbContext db, ITenantContext tenant, ICustomerDirectory customers, TimeProvider clock)
{
    public static readonly Error ProfileRequired = Error.Validation(
        "CUSTOMER_PROFILE_REQUIRED", "Uzupełnij imię w swoim profilu, zanim wystawisz opinię.");

    /// <summary>
    /// The signed-in customer reviews one of their completed visits: once, within 90 days. Their name comes from
    /// their profile and is shortened to first name and initial.
    /// </summary>
    public async Task<Result<ReviewDto>> HandleAsync(SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        var userId = tenant.UserId ?? throw new InvalidOperationException("A review needs a signed-in customer.");

        // Matched on the caller, so another customer's visit is simply not found
        var visit = await db.ReviewableVisits
            .FirstOrDefaultAsync(v => v.BookingId == request.BookingId && v.CustomerUserId == userId, cancellationToken);
        if (visit == null) return ReviewErrors.VisitNotFound;

        if (await db.Reviews.AnyAsync(r => r.BookingId == visit.BookingId, cancellationToken)) return ReviewErrors.AlreadyReviewed;

        var profile = await customers.FindProfileAsync(userId, cancellationToken);
        if (profile == null) return ProfileRequired;

        var review = visit.WriteReview(request.Rating, request.Comment, profile.Name, clock.GetUtcNow().UtcDateTime);
        if (!review.Success) return review.Error!;

        db.Reviews.Add(review.Value!);
        await db.SaveChangesAsync(cancellationToken);

        return ReviewDto.From(review.Value!);
    }
}

public sealed class GetReviewableVisitsHandler(IMarketplaceDbContext db, ITenantContext tenant, TimeProvider clock)
{
    /// <summary>
    /// The caller's completed visits they haven't reviewed yet and still can, newest first.
    /// </summary>
    public async Task<List<ReviewableVisitDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = tenant.UserId ?? throw new InvalidOperationException("This needs a signed-in customer.");
        var since = clock.GetUtcNow().UtcDateTime - ReviewableVisit.ReviewPeriod;

        var visits = await db.ReviewableVisits.AsNoTracking()
            .Where(v => v.CustomerUserId == userId && v.CompletedAt >= since
                        && !db.Reviews.Any(r => r.BookingId == v.BookingId))
            .OrderByDescending(v => v.CompletedAt).ThenBy(v => v.BookingId)
            .Take(50)
            .ToListAsync(cancellationToken);

        var studioIds = visits.Select(v => v.StudioId).Distinct().ToList();
        var names = await db.Listings.AsNoTracking()
            .Where(l => studioIds.Contains(l.StudioId))
            .ToDictionaryAsync(l => l.StudioId, l => l.Name, cancellationToken);

        return visits.Select(v => new ReviewableVisitDto(
            v.BookingId, v.StudioId, names.GetValueOrDefault(v.StudioId), v.CompletedAt,
            v.CompletedAt + ReviewableVisit.ReviewPeriod)).ToList();
    }
}

public sealed class GetStudioReviewsHandler(IMarketplaceDbContext db)
{
    /// <summary>
    /// A published studio's reviews, newest first.
    /// </summary>
    public async Task<Result<PagedResponse<ReviewDto>>> HandleAsync(
        Guid studioId, PageRequest page, CancellationToken cancellationToken)
    {
        if (!await db.Listings.AnyAsync(l => l.StudioId == studioId && l.IsPublished, cancellationToken))
            return ListingErrors.NotFound;

        var reviews = await db.Reviews.AsNoTracking()
            .Where(r => r.StudioId == studioId)
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .ToPagedResponseAsync(page, cancellationToken);

        return reviews.Map(ReviewDto.From);
    }
}
