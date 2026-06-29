using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OrderSphere.Invoicing.Infrastructure.Pdf;

public sealed class QuestPdfInvoiceService : IInvoicePdfService
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public Task<byte[]> GenerateAsync(Invoice invoice, CancellationToken ct = default)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Column(col =>
                {
                    col.Item().Text("OrderSphere").Bold().FontSize(22).FontColor("#6366F1");
                    col.Item().PaddingTop(4).Text($"Rechnung {invoice.InvoiceNumber}")
                        .Bold().FontSize(16);
                    col.Item().PaddingTop(2).LineHorizontal(1).LineColor("#E5E7EB");
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Rechnungsdatum").Bold();
                            c.Item().Text(invoice.IssuedAt.ToString("dd.MM.yyyy", De));
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Bestellnummer").Bold();
                            c.Item().Text(invoice.OrderId.ToString());
                        });
                    });

                    col.Item().PaddingTop(12).Text("Rechnungsempfänger").Bold();
                    col.Item().Text(invoice.CustomerName);
                    col.Item().Text(invoice.CustomerEmail);

                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(5);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#F3F4F6").Padding(6).Text("Produkt").Bold();
                            header.Cell().Background("#F3F4F6").Padding(6).AlignCenter().Text("Menge").Bold();
                            header.Cell().Background("#F3F4F6").Padding(6).AlignRight().Text("Einzelpreis").Bold();
                            header.Cell().Background("#F3F4F6").Padding(6).AlignRight().Text("Gesamt").Bold();
                        });

                        foreach (var item in invoice.Items)
                        {
                            var lineTotal = item.Quantity * item.UnitPrice;
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(6).Text(item.ProductName);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(6).AlignCenter().Text(item.Quantity.ToString());
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(6).AlignRight().Text(item.UnitPrice.ToString("C", De));
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(6).AlignRight().Text(lineTotal.ToString("C", De));
                        }
                    });

                    col.Item().PaddingTop(8).AlignRight()
                        .Text($"Gesamtbetrag: {invoice.Total.ToString("C", De)}")
                        .Bold().FontSize(14);
                });

                page.Footer().AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Seite ");
                        x.CurrentPageNumber();
                        x.Span(" von ");
                        x.TotalPages();
                    });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }
}
