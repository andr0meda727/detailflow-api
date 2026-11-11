using Notifications.Infrastructure.Reports;
using Scheduling.Contracts;
using Shared.Kernel.Time;

namespace Unit.Modules.Notifications;

[TestFixture]
public class BookingsReportPdfTests
{
    private static readonly DateOnly From = new(2026, 10, 1);
    private static readonly DateOnly To = new(2026, 10, 31);
    private static readonly DateTime Now = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeZoneInfo Warsaw = TimeZones.Find("Europe/Warsaw");

    // A PDF file starts with %PDF
    private static bool IsPdf(byte[] bytes) => bytes.AsSpan().StartsWith("%PDF"u8);

    [Test]
    public void Renders_AnEmptyPeriod()
    {
        Assert.That(IsPdf(new BookingsReportPdf().Render([], From, To, Warsaw, Now)), Is.True);
    }

    [Test]
    public void Renders_ManyPagesWithPolishTextAndBookingsWithoutServices()
    {
        var start = new DateTime(2026, 10, 1, 6, 0, 0, DateTimeKind.Utc);
        var rows = Enumerable.Range(0, 120)
            .Select(i => new BookingReportRow(start.AddHours(i), $"Łukasz Żółć {i}",
                i % 3 == 0 ? [] : ["Polerowanie", "Woskowanie"], "Zakończona"))
            .Reverse()
            .ToList();

        var pdf = new BookingsReportPdf().Render(rows, From, To, Warsaw, Now);

        Assert.That(IsPdf(pdf), Is.True);
    }

    [Test]
    public void ServicesCell_ListsEveryService()
    {
        Assert.That(BookingsReportPdf.ServicesCell(["Polerowanie", "Woskowanie"]), Is.EqualTo("Polerowanie, Woskowanie"));
        Assert.That(BookingsReportPdf.ServicesCell([]), Is.EqualTo("—"));
    }
}
