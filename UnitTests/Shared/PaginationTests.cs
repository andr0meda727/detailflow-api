using System.ComponentModel.DataAnnotations;
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
}
