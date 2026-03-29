using KiranaStore.Application.DTOs;
using KiranaStore.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static System.Net.Mime.MediaTypeNames;

namespace KiranaStore.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    public Task<byte[]> GeneratePdfAsync(OrderDto order, string storeName, string storeAddress, string storeGstin)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, order, storeName, storeAddress, storeGstin));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();

        return Task.FromResult(pdf);
    }

    private static void ComposeHeader(IContainer c)
    {
        c.PaddingBottom(10).BorderBottom(1).BorderColor("#e2e8f0").Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("TAX INVOICE").FontSize(20).Bold().FontColor("#4f46e5");
                col.Item().Text("Original for Recipient").FontSize(9).FontColor("#94a3b8");
            });
            row.ConstantItem(120).AlignRight().Column(col =>
            {
                col.Item().Text(t => { t.Span("KIRANA STORE").Bold().FontSize(14).FontColor("#1e293b"); });
                col.Item().Text("GST Compliant Invoice").FontSize(9).FontColor("#64748b");
            });
        });
    }

    private static void ComposeContent(IContainer c, OrderDto order, string storeName, string storeAddress, string storeGstin)
    {
        c.Column(col =>
        {
            // Invoice meta
            col.Item().PaddingVertical(12).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text($"Invoice No: {order.InvoiceNumber}").Bold().FontSize(11);
                    left.Item().Text($"Date: {order.OrderDate:dd MMM yyyy, HH:mm}").FontColor("#64748b");
                    left.Item().Text($"Payment: {order.PaymentMethod}").FontColor("#64748b");
                });
                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().Text("Bill To:").Bold().FontSize(10);
                    right.Item().Text(order.CustomerName).FontSize(11).Bold().FontColor("#4f46e5");
                });
            });

            // Items table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(30);
                    cols.RelativeColumn(4);
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                // Header
                static IContainer HeaderCell(IContainer c) => c
                    .Background("#4f46e5").Padding(6);
                static void HeaderText(TextDescriptor t, string text)
                {
                    t.Span(text)
                     .FontColor(Colors.White)
                     .Bold()
                     .FontSize(9);
                }

                table.Header(h =>
                {
                    h.Cell().Element(HeaderCell).Text(t => HeaderText(t, "#"));
                    h.Cell().Element(HeaderCell).Text(t => HeaderText(t, "Product"));
                    h.Cell().Element(HeaderCell).AlignRight().Text(t => HeaderText(t, "Price"));
                    h.Cell().Element(HeaderCell).AlignRight().Text(t => HeaderText(t, "Qty"));
                    h.Cell().Element(HeaderCell).AlignRight().Text(t => HeaderText(t, "GST%"));
                    h.Cell().Element(HeaderCell).AlignRight().Text(t => HeaderText(t, "GST₹"));
                    h.Cell().Element(HeaderCell).AlignRight().Text(t => HeaderText(t, "Total"));
                });

                int i = 1;
                foreach (var item in order.Items)
                {
                    var bg = i % 2 == 0 ? "#f8fafc" : "#ffffff";
                    static IContainer Cell(IContainer c, string bg) => c.Background(bg).BorderBottom(1).BorderColor("#e2e8f0").Padding(6);

                    table.Cell().Element(c => Cell(c, bg)).Text($"{i}");
                    table.Cell().Element(c => Cell(c, bg)).Text(item.ProductName);
                    table.Cell().Element(c => Cell(c, bg)).AlignRight().Text($"₹{item.UnitPrice:F2}");
                    table.Cell().Element(c => Cell(c, bg)).AlignRight().Text($"{item.Quantity}");
                    table.Cell().Element(c => Cell(c, bg)).AlignRight().Text($"{item.GSTRate}%");
                    table.Cell().Element(c => Cell(c, bg)).AlignRight().Text($"₹{item.GSTAmount:F2}");
                    table.Cell().Element(c => Cell(c, bg)).AlignRight().Text($"₹{item.Total:F2}").Bold();
                    i++;
                }
            });

            // Totals
            col.Item().PaddingTop(12).AlignRight().Width(240).Column(totals =>
            {
                void TotalRow(string label, decimal amount, bool bold = false, string color = "#1e293b")
                {
                    totals.Item().Row(r =>
                    {
                        // Label
                        r.RelativeItem().Text(label).FontColor("#64748b");

                        // Amount (Correct Bold Handling)
                        r.ConstantItem(100).AlignRight().Text(t =>
                        {
                            var span = t.Span($"₹{amount:F2}").FontColor(color);

                            if (bold)
                                span.Bold();
                        });
                    });

                    totals.Item().PaddingVertical(2)
                        .LineHorizontal(0.5f)
                        .LineColor("#e2e8f0");
                }

                TotalRow("Subtotal", order.SubTotal);
                TotalRow("GST", order.GSTAmount);
                TotalRow("Discount", order.DiscountAmount);

                totals.Item().Background("#4f46e5").Padding(8).Row(r =>
                {
                    r.RelativeItem()
                        .Text("TOTAL AMOUNT")
                        .FontColor(Colors.White)
                        .Bold()
                        .FontSize(12);

                    r.ConstantItem(100).AlignRight()
                        .Text($"₹{order.TotalAmount:F2}")
                        .FontColor(Colors.White)
                        .Bold()
                        .FontSize(13);
                });
            });
            // GST Summary
            col.Item().PaddingTop(20).Text("GST Summary").Bold().FontSize(10).FontColor("#64748b");
            col.Item().Table(gst =>
            {
                gst.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                static IContainer GHead(IContainer c) => c.Background("#f1f5f9").Padding(5);
                gst.Header(h =>
                {
                    h.Cell().Element(GHead).Text("GST Rate").Bold();
                    h.Cell().Element(GHead).AlignRight().Text("Taxable Amt").Bold();
                    h.Cell().Element(GHead).AlignRight().Text("CGST").Bold();
                    h.Cell().Element(GHead).AlignRight().Text("SGST").Bold();
                });
                var gstGroups = order.Items.GroupBy(x => x.GSTRate);
                foreach (var g in gstGroups)
                {
                    var taxable = g.Sum(x => x.UnitPrice * x.Quantity);
                    var gstAmt  = g.Sum(x => x.GSTAmount);
                    gst.Cell().Padding(5).Text($"{g.Key}%");
                    gst.Cell().Padding(5).AlignRight().Text($"₹{taxable:F2}");
                    gst.Cell().Padding(5).AlignRight().Text($"₹{gstAmt / 2:F2}");
                    gst.Cell().Padding(5).AlignRight().Text($"₹{gstAmt / 2:F2}");
                }
            });
        });
    }

    private static void ComposeFooter(IContainer c)
    {
        c.BorderTop(1).BorderColor("#e2e8f0").PaddingTop(8).Row(row =>
        {
            row.RelativeItem().Text("Thank you for your business! 🙏").FontColor("#64748b").FontSize(9);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Generated by KiranaStore ").FontColor("#94a3b8").FontSize(8);
                t.Span("• ").FontColor("#94a3b8").FontSize(8);
                t.CurrentPageNumber().FontColor("#94a3b8").FontSize(8);
            });
        });
    }
}
