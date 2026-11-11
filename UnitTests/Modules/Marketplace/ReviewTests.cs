using Marketplace.Domain.Reviews;

namespace Unit.Modules.Marketplace;

[TestFixture]
public class ReviewTests
{
    private static readonly DateTime CompletedAt = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private static ReviewableVisit Visit() => ReviewableVisit.Record(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CompletedAt);

    [TestCase("Anna Nowak", "Anna N.")]
    [TestCase("  jan   maria  kowalski ", "jan K.")]
    [TestCase("Ola", "Ola")]
    [TestCase("   ", "Klient")]
    public void AuthorName_IsTheFirstNameAndAnInitial(string fullName, string expected)
    {
        Assert.That(Review.ShortName(fullName), Is.EqualTo(expected));
    }

    [Test]
    public void AVisit_CanBeReviewedWithin90Days()
    {
        var visit = Visit();

        var result = visit.WriteReview(5, "  Świetna robota ", "Anna Nowak", CompletedAt.AddDays(89));

        Assert.That(result.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value!.BookingId, Is.EqualTo(visit.BookingId));
            Assert.That(result.Value.StudioId, Is.EqualTo(visit.StudioId));
            Assert.That(result.Value.Comment, Is.EqualTo("Świetna robota"));
            Assert.That(result.Value.AuthorName, Is.EqualTo("Anna N."));
        });
    }

    [Test]
    public void AVisit_CannotBeReviewedAfter90Days()
    {
        var result = Visit().WriteReview(4, null, "Anna", CompletedAt.AddDays(91));

        Assert.That(result.Error?.Code, Is.EqualTo("REVIEW_PERIOD_OVER"));
    }

    [TestCase(0)]
    [TestCase(6)]
    public void Rating_IsOneToFive(int rating)
    {
        Assert.That(() => Visit().WriteReview(rating, null, "Anna", CompletedAt), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ABlankComment_IsNoComment()
    {
        Assert.That(Visit().WriteReview(3, "   ", "Anna", CompletedAt).Value!.Comment, Is.Null);
    }
}
