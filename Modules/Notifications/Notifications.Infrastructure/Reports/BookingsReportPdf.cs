using Scheduling.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Notifications.Infrastructure.Reports;

/// <summary>
/// Renders the bookings of a period as a PDF (QuestPDF, community licence).
/// </summary>
public class BookingsReportPdf
{
    public byte[] Render(
        IEnumerable<BookingReportRow> bookings, DateTime dateFrom, DateTime dateTo)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var bookingList = bookings.ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("DetailFlow — Raport wizyt")
                        .Bold().FontSize(18);
                    col.Item().Text(
                        $"Okres: {dateFrom:dd.MM.yyyy} — {dateTo:dd.MM.yyyy}")
                        .FontSize(11).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(4).LineHorizontal(1)
                        .LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    // Podsumowanie
                    col.Item().Background(Colors.Grey.Lighten4).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Liczba wizyt").FontColor(Colors.Grey.Medium);
                            c.Item().Text(bookingList.Count.ToString()).Bold().FontSize(20);
                        });
                    });

                    col.Item().PaddingTop(16);

                    if (!bookingList.Any())
                    {
                        col.Item().Text("Brak wizyt w wybranym okresie.")
                            .FontColor(Colors.Grey.Medium);
                        return;
                    }

                    // Tabela
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2); // data
                            cols.RelativeColumn(3); // klient
                            cols.RelativeColumn(3); // usługa
                            cols.RelativeColumn(2); // status
                        });

                        // Nagłówek
                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Data", "Klient", "Usługa", "Status" })
                            {
                                header.Cell().Background(Colors.Grey.Lighten3)
                                    .Padding(6).Text(h).Bold();
                            }
                        });

                        // Wiersze
                        // Stripe by position in the sorted table, not in the unsorted input
                        foreach (var (booking, index) in bookingList.OrderBy(b => b.ScheduledAt).Select((b, i) => (b, i)))
                        {
                            var bg = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;

                            table.Cell().Background(bg).Padding(6)
                                .Text(booking.ScheduledAt.ToString("dd.MM.yyyy HH:mm"));
                            table.Cell().Background(bg).Padding(6)
                                .Text(booking.ClientName);
                            table.Cell().Background(bg).Padding(6)
                                .Text(ServicesCell(booking.Services));
                            table.Cell().Background(bg).Padding(6)
                                .Text(booking.Status);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Wygenerowano: ");
                    text.Span(DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm"));
                    text.Span(" UTC  |  Strona ");
                    text.CurrentPageNumber();
                    text.Span(" z ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    /// <summary>
    /// A booking can have several services; the report lists all of them, not just the first.
    /// </summary>
    public static string ServicesCell(IReadOnlyList<string> services) =>
        services.Count == 0 ? "—" : string.Join(", ", services);
}