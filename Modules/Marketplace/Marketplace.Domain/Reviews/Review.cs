using Shared.Kernel.Auditing;
using Shared.Kernel.Results;

namespace Marketplace.Domain.Reviews;

/// <summary>
/// A completed visit its customer may review, recorded from Scheduling's BookingCompleted. Only visits booked from a
/// customer account get one, so every review comes from someone who really was at the studio.
/// </summary>
public class ReviewableVisit : IAuditableEntity
{
    // How long after the visit a review is still accepted
    public static readonly TimeSpan ReviewPeriod = TimeSpan.FromDays(90);

    // For EF Core
    private ReviewableVisit() { }

    public Guid BookingId { get; private set; }

    public Guid StudioId { get; private set; }

    public Guid CustomerUserId { get; private set; }

    public DateTime CompletedAt { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static ReviewableVisit Record(Guid bookingId, Guid studioId, Guid customerUserId, DateTime completedAt)
    {
        if (bookingId == Guid.Empty || studioId == Guid.Empty || customerUserId == Guid.Empty)
            throw new ArgumentException("A reviewable visit needs its booking, studio and customer.");

        return new ReviewableVisit
        {
            BookingId = bookingId, StudioId = studioId, CustomerUserId = customerUserId, CompletedAt = completedAt
        };
    }

    public bool IsOpenForReview(DateTime now) => now <= CompletedAt + ReviewPeriod;

    /// <summary>
    /// The customer's review of this visit, if it is still open for one.
    /// </summary>
    public Result<Review> WriteReview(int rating, string? comment, string authorName, DateTime now)
    {
        if (!IsOpenForReview(now)) return ReviewErrors.PeriodOver;

        return Review.Write(this, rating, comment, authorName);
    }
}

/// <summary>
/// One customer's rating (1–5) and optional comment about one visit; one per booking. It shows on the studio's page
/// with the author's first name and initial only.
/// </summary>
public class Review : IAuditableEntity
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int MaxCommentLength = 1000;

    // For EF Core
    private Review() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; private set; }

    public Guid BookingId { get; private set; }

    public Guid CustomerUserId { get; private set; }

    // "Anna N.", taken from the customer's profile when they wrote it
    public string AuthorName { get; private set; } = null!;

    public int Rating { get; private set; }

    public string? Comment { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    internal static Review Write(ReviewableVisit visit, int rating, string? comment, string authorName)
    {
        if (rating is < MinRating or > MaxRating) throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating is 1 to 5.");
        var trimmed = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (trimmed?.Length > MaxCommentLength) throw new ArgumentException("The comment is too long.", nameof(comment));

        return new Review
        {
            Id = Guid.NewGuid(), StudioId = visit.StudioId, BookingId = visit.BookingId,
            CustomerUserId = visit.CustomerUserId, AuthorName = ShortName(authorName), Rating = rating, Comment = trimmed
        };
    }

    /// <summary>
    /// "Anna Nowak" becomes "Anna N.": enough to feel personal, not enough to identify someone.
    /// </summary>
    public static string ShortName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => "Klient",
            1 => parts[0],
            _ => $"{parts[0]} {char.ToUpperInvariant(parts[^1][0])}."
        };
    }
}

public static class ReviewErrors
{
    public static readonly Error VisitNotFound = Error.NotFound(
        "REVIEWABLE_VISIT_NOT_FOUND", "Nie znaleziono zakończonej wizyty, którą możesz ocenić.");

    public static readonly Error AlreadyReviewed = Error.Conflict(
        "REVIEW_ALREADY_EXISTS", "Ta wizyta została już przez Ciebie oceniona.");

    public static readonly Error PeriodOver = Error.Validation(
        "REVIEW_PERIOD_OVER", "Wizytę można ocenić w ciągu 90 dni od jej zakończenia.");
}
