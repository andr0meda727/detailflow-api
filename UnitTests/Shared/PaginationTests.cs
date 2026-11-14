using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Shared.Kernel.Pagination;

namespace Unit.Shared;

[TestFixture]
public class PaginationTests
{
    [Test]
    public void PageRequest_DefaultsToFirstPageOfTwenty()
    {
        var page = new PageRequest();

        Assert.That(page.Page, Is.EqualTo(1));
        Assert.That(page.PageSize, Is.EqualTo(PageRequest.DefaultPageSize));
        Assert.That(page.Skip, Is.Zero);
    }

    [Test]
    public void PageRequest_SkipsPreviousPages()
    {
        Assert.That(new PageRequest { Page = 3, PageSize = 25 }.Skip, Is.EqualTo(50));
    }

    [TestCase(0, 20)]
    [TestCase(1, 0)]
    [TestCase(1, PageRequest.MaxPageSize + 1)]
    [TestCase(PageRequest.MaxPage + 1, 20)]
    public void PageRequest_RejectsOutOfRangeValues(int page, int pageSize)
    {
        var request = new PageRequest { Page = page, PageSize = pageSize };

        var valid = Validator.TryValidateObject(request, new ValidationContext(request), [], validateAllProperties: true);

        Assert.That(valid, Is.False);
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(20, 1)]
    [TestCase(21, 2)]
    public void PagedResponse_RoundsTotalPagesUp(int totalCount, int expectedPages)
    {
        var response = new PagedResponse<int>([], 1, 20, totalCount);

        Assert.That(response.TotalPages, Is.EqualTo(expectedPages));
    }

    [Test]
    public void PagedResponse_Map_KeepsPagingMetadata()
    {
        var mapped = new PagedResponse<int>([1, 2], 2, 2, 5).Map(i => i.ToString());

        Assert.That(mapped.Items, Is.EqualTo(new[] { "1", "2" }));
        Assert.That(mapped, Has.Property(nameof(mapped.Page)).EqualTo(2)
            .And.Property(nameof(mapped.PageSize)).EqualTo(2)
            .And.Property(nameof(mapped.TotalCount)).EqualTo(5));
    }

    [Test]
    public void CursorRequest_DefaultsToTwenty()
    {
        Assert.That(new CursorRequest().Limit, Is.EqualTo(CursorRequest.DefaultLimit));
    }

    [TestCase(0)]
    [TestCase(CursorRequest.MaxLimit + 1)]
    public void CursorRequest_RejectsOutOfRangeLimits(int limit)
    {
        var request = new CursorRequest { Limit = limit };

        var valid = Validator.TryValidateObject(request, new ValidationContext(request), [], validateAllProperties: true);

        Assert.That(valid, Is.False);
    }

    [Test]
    public void TimeCursor_RoundTrips()
    {
        var cursor = new TimeCursor(new DateTime(2026, 11, 14, 9, 30, 15, 123, 456, DateTimeKind.Utc), Guid.NewGuid());

        Assert.That(TimeCursor.TryDecode(cursor.Encode(), out var decoded), Is.True);
        Assert.That(decoded, Is.EqualTo(cursor));
        Assert.That(decoded.At.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void TimeCursor_IsUrlSafe()
    {
        var encoded = new TimeCursor(DateTime.UtcNow, Guid.NewGuid()).Encode();

        Assert.That(encoded, Does.Match("^[A-Za-z0-9_-]+$"));
    }

    [TestCase("")]
    [TestCase("not base64!")]
    public void TimeCursor_RejectsGarbage(string value)
    {
        Assert.That(TimeCursor.TryDecode(value, out _), Is.False);
    }

    [TestCase("123")]
    [TestCase("-1:0123456789abcdef0123456789abcdef")]
    [TestCase("99999999999999999999:0123456789abcdef0123456789abcdef")]
    [TestCase("3155378976000000000:0123456789abcdef0123456789abcdef")] // DateTime.MaxValue.Ticks + 1
    [TestCase("638000000000000000:not-a-guid")]
    [TestCase("638000000000000000:0123456789abcdef0123456789abcdef:x")]
    public void TimeCursor_RejectsMalformedContent(string text)
    {
        var encoded = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(text));

        Assert.That(TimeCursor.TryDecode(encoded, out _), Is.False);
    }

    [Test]
    public void CursorResponse_Map_KeepsTheNextCursor()
    {
        var mapped = new CursorResponse<int>([1, 2], "abc").Map(i => i.ToString());

        Assert.That(mapped.Items, Is.EqualTo(new[] { "1", "2" }));
        Assert.That(mapped.NextCursor, Is.EqualTo("abc"));
    }
}
