using Notifications.Infrastructure.Reports;
using Scheduling.Contracts;

namespace Unit.Modules.Notifications;

[TestFixture]
public class BookingsReportPdfTests
{
    private static readonly DateTime From = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To = new(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc);

    // A PDF file starts with %PDF
    private static bool IsPdf(byte[] bytes) => bytes.AsSpan().StartsWith("%PDF"u8);

    [Test]
    public void Renders_AnEmptyPeriod()
    {
        Assert.That(IsPdf(new BookingsReportPdf().Render([], From, To)), Is.True);
    }

    [Test]
    public void Renders_ManyPagesWithPolishTextAndBookingsWithoutServices()
    {
        var rows = Enumerable.Range(0, 120)
            .Select(i => new BookingReportRow(From.AddHours(i), $"Łukasz Żółć {i}",
                i % 3 == 0 ? [] : ["Polerowanie", "Woskowanie"], "Zakończona"))
            .Reverse()
            .ToList();

        var pdf = new BookingsReportPdf().Render(rows, From, To);

        Assert.That(IsPdf(pdf), Is.True);
    }

    [Test]
    public void ServicesCell_ListsEveryService()
    {
        Assert.That(BookingsReportPdf.ServicesCell(["Polerowanie", "Woskowanie"]), Is.EqualTo("Polerowanie, Woskowanie"));
        Assert.That(BookingsReportPdf.ServicesCell([]), Is.EqualTo("—"));
    }
}
