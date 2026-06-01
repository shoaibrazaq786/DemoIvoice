using PakAccountingERP.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PakAccountingERP.Services;

public class InvoicePdfService
{
    public InvoicePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(SalesInvoice invoice, Company company)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(company.CompanyName).Bold().FontSize(16).FontColor(Colors.Red.Darken3);
                            c.Item().Text(company.Address ?? "");
                            c.Item().Text($"NTN: {company.NTN} | STRN: {company.STRN}");
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("SALES INVOICE").Bold().FontSize(14);
                            c.Item().Text($"Invoice #: {invoice.InvoiceNumber}");
                            c.Item().Text($"FBR #: {invoice.FbrInvoiceNumber ?? "Pending"}");
                            c.Item().Text($"Date: {invoice.InvoiceDate:dd-MMM-yyyy}");
                        });
                    });
                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Text($"Buyer: {invoice.Customer?.BuyerName}").Bold();
                    col.Item().Text(invoice.BuyerAddress ?? "");
                    col.Item().Text($"NTN/CNIC: {invoice.BuyerNTN ?? invoice.BuyerCNIC}");
                    col.Item().PaddingVertical(10);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(25);
                            c.RelativeColumn(3);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Red.Darken3).Padding(4).Text("#").FontColor(Colors.White);
                            h.Cell().Background(Colors.Red.Darken3).Padding(4).Text("Description").FontColor(Colors.White);
                            h.Cell().Background(Colors.Red.Darken3).Padding(4).Text("HS Code").FontColor(Colors.White);
                            h.Cell().Background(Colors.Red.Darken3).Padding(4).Text("Qty").FontColor(Colors.White);
                            h.Cell().Background(Colors.Red.Darken3).Padding(4).Text("Price").FontColor(Colors.White);
                            h.Cell().Background(Colors.Red.Darken3).Padding(4).Text("Tax").FontColor(Colors.White);
                            h.Cell().Background(Colors.Red.Darken3).Padding(4).Text("Total").FontColor(Colors.White);
                        });

                        var idx = 1;
                        foreach (var line in invoice.Items)
                        {
                            table.Cell().Padding(4).Text(idx++.ToString());
                            table.Cell().Padding(4).Text(line.ProductDescription);
                            table.Cell().Padding(4).Text(line.HSCode ?? "");
                            table.Cell().Padding(4).Text($"{line.Quantity:N2}");
                            table.Cell().Padding(4).Text($"Rs. {line.Price:N2}");
                            table.Cell().Padding(4).Text($"Rs. {line.TaxAmount:N2}");
                            table.Cell().Padding(4).Text($"Rs. {line.LineTotal:N2}");
                        }
                    });

                    col.Item().AlignRight().PaddingTop(15).Column(totals =>
                    {
                        totals.Item().Text($"Sub Total: Rs. {invoice.SubTotal:N2}");
                        totals.Item().Text($"Tax: Rs. {invoice.TaxAmount:N2}");
                        totals.Item().Text($"Net Total: Rs. {invoice.NetTotal:N2}").Bold().FontSize(12);
                    });

                    if (!string.IsNullOrEmpty(invoice.FbrQrCodeData))
                    {
                        col.Item().PaddingTop(20).Text("FBR Digital Invoice - Verified").Italic();
                        col.Item().Text($"QR Data: {invoice.FbrQrCodeData}").FontSize(8);
                    }
                });

                page.Footer().AlignCenter().Text(t => { t.Span("Pak Accounting ERP | FBR Digital Invoicing"); });
            });
        });

        return document.GeneratePdf();
    }
}
