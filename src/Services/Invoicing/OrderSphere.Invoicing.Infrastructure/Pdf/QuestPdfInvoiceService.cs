using System.Globalization;
using OrderSphere.Invoicing.Domain.Enums;
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

                    col.Item().PaddingTop(8).AlignRight().Column(c =>
                    {
                        c.Item().Text($"Netto: {invoice.NetAmount.ToString("C", De)}");
                        c.Item().Text($"MwSt ({invoice.TaxRate.ToString("P0", De)}): {invoice.TaxAmount.ToString("C", De)}");
                        c.Item().PaddingTop(2).Text($"Gesamtbetrag: {invoice.Total.ToString("C", De)}")
                            .Bold().FontSize(14);
                    });

                    if (invoice.Adjustments.Count > 0)
                    {
                        col.Item().PaddingTop(20).Text("Anpassungen").Bold();

                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(4);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#F3F4F6").Padding(6).Text("Typ").Bold();
                                header.Cell().Background("#F3F4F6").Padding(6).Text("Begründung").Bold();
                                header.Cell().Background("#F3F4F6").Padding(6).AlignRight().Text("Betrag").Bold();
                                header.Cell().Background("#F3F4F6").Padding(6).Text("Datum").Bold();
                            });

                            foreach (var adjustment in invoice.Adjustments.OrderBy(a => a.AppliedAt))
                            {
                                var typeLabel = adjustment.Type == InvoiceAdjustmentType.Discount ? "Rabatt" : "Gutschrift";
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(6).Text(typeLabel);
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(6).Text(adjustment.Reason);
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(6).AlignRight()
                                    .Text($"-{adjustment.AmountNet.ToString("C", De)}");
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(6)
                                    .Text(adjustment.AppliedAt.ToString("dd.MM.yyyy", De));
                            }
                        });

                        col.Item().PaddingTop(8).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Ursprünglicher Betrag: {invoice.Total.ToString("C", De)}");
                            c.Item().Text($"Angepasste MwSt: {invoice.AdjustedTax.ToString("C", De)}");
                            c.Item().PaddingTop(2).Text($"Angepasster Gesamtbetrag: {invoice.AdjustedTotal.ToString("C", De)}")
                                .Bold().FontSize(14);
                        });
                    }
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
