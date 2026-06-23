using System.Globalization;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Invoices;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FreshCart.Reporting.Infrastructure.Documents.Pdf;

/// <summary>
/// QuestPDF-based invoice renderer. Produces an A4 PDF with a professional layout: header,
/// billing &amp; shipping addresses, itemised lines, totals block, payment-due footer.
/// </summary>
/// <remarks>
/// QuestPDF runs entirely in managed code with no native PDF binaries; safe in chiselled Linux
/// containers. The Community licence is set in the composition root at startup.
/// </remarks>
public sealed class QuestPdfInvoiceRenderer : IInvoiceRenderer
{
    private static readonly CultureInfo InvoiceCulture = CultureInfo.GetCultureInfo("en-US");

    public InvoiceRenderingFormat Format => InvoiceRenderingFormat.Pdf;

    public Task<RenderedDocument> RenderAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        cancellationToken.ThrowIfCancellationRequested();

        var pdfBytes = Document
            .Create(documentContainer => documentContainer.Page(pageDescriptor =>
            {
                pageDescriptor.Margin(36);
                pageDescriptor.Size(PageSizes.A4);
                pageDescriptor.PageColor(Colors.White);
                pageDescriptor.DefaultTextStyle(textStyle => textStyle.FontSize(10).FontFamily(Fonts.Calibri));

                pageDescriptor.Header().Element(headerContainer => ComposeHeader(headerContainer, invoice));
                pageDescriptor.Content().Element(contentContainer => ComposeBody(contentContainer, invoice));
                pageDescriptor.Footer().Element(footerContainer => ComposeFooter(footerContainer, invoice));
            }))
            .GeneratePdf();

        var fileName = $"{invoice.InvoiceNumber}.pdf";
        return Task.FromResult(new RenderedDocument(fileName, "application/pdf", pdfBytes));
    }

    private static void ComposeHeader(IContainer headerContainer, Invoice invoice)
    {
        headerContainer.Row(rowDescriptor =>
        {
            rowDescriptor.RelativeItem().Column(brandingColumn =>
            {
                brandingColumn.Item().Text("FreshCart").Bold().FontSize(20).FontColor("#27ae60");
                brandingColumn.Item().Text("Online supermarket").FontSize(9).FontColor(Colors.Grey.Darken1);
                brandingColumn.Item().PaddingTop(2).Text("https://freshcart.com").FontSize(9).FontColor(Colors.Grey.Medium);
            });

            rowDescriptor.RelativeItem().Column(invoiceMetaColumn =>
            {
                invoiceMetaColumn.Item().AlignRight().Text(InvoiceTitleFor(invoice.Kind)).Bold().FontSize(18);
                invoiceMetaColumn.Item().AlignRight().Text(invoice.InvoiceNumber).FontSize(11).FontColor(Colors.Grey.Darken1);
                invoiceMetaColumn.Item().AlignRight().Text($"Issued: {invoice.IssuedOnUtc.UtcDateTime:yyyy-MM-dd}").FontSize(9);
                if (invoice.DueOnUtc is not null)
                {
                    invoiceMetaColumn.Item().AlignRight().Text($"Due: {invoice.DueOnUtc.Value.UtcDateTime:yyyy-MM-dd}").FontSize(9);
                }
            });
        });
    }

    private static void ComposeBody(IContainer bodyContainer, Invoice invoice)
    {
        bodyContainer.PaddingVertical(15).Column(rootColumn =>
        {
            rootColumn.Item().Row(addressRow =>
            {
                addressRow.RelativeItem().Element(cell => ComposeAddressBlock(cell, "Bill to", invoice.BillingAddress, invoice.CustomerEmail));
                addressRow.ConstantItem(15);
                addressRow.RelativeItem().Element(cell => ComposeAddressBlock(cell, "Ship to", invoice.ShippingAddress, customerEmail: null));
            });

            rootColumn.Item().PaddingTop(20).Element(linesContainer => ComposeLineTable(linesContainer, invoice));

            rootColumn.Item().PaddingTop(20).AlignRight().Column(totalsColumn =>
            {
                AppendTotalsRow(totalsColumn, "Subtotal",  invoice.Subtotal,      invoice.CurrencyCode);
                AppendTotalsRow(totalsColumn, "Discounts", -invoice.DiscountTotal, invoice.CurrencyCode);
                AppendTotalsRow(totalsColumn, "Tax",       invoice.TaxTotal,       invoice.CurrencyCode);
                AppendTotalsRow(totalsColumn, "Shipping",  invoice.ShippingTotal,  invoice.CurrencyCode);
                totalsColumn.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                totalsColumn.Item().PaddingTop(4).Row(grandTotalRow =>
                {
                    grandTotalRow.RelativeItem().AlignRight().Text("Grand total").Bold();
                    grandTotalRow.ConstantItem(110).AlignRight().Text(FormatMoney(invoice.GrandTotal, invoice.CurrencyCode)).Bold();
                });
            });

            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                rootColumn.Item().PaddingTop(20).Background(Colors.Grey.Lighten4).Padding(10).Column(notesColumn =>
                {
                    notesColumn.Item().Text("Notes").Bold().FontSize(10);
                    notesColumn.Item().PaddingTop(4).Text(invoice.Notes).FontSize(9);
                });
            }
        });
    }

    private static void ComposeAddressBlock(IContainer container, string heading, InvoiceAddress address, string? customerEmail)
    {
        container.Background(Colors.Grey.Lighten4).Padding(10).Column(addressColumn =>
        {
            addressColumn.Item().Text(heading).Bold().FontSize(10);
            addressColumn.Item().PaddingTop(4).Text(address.FullName);
            addressColumn.Item().Text(address.AddressLine1);
            if (!string.IsNullOrWhiteSpace(address.AddressLine2))
            {
                addressColumn.Item().Text(address.AddressLine2);
            }
            addressColumn.Item().Text($"{address.City}{(string.IsNullOrWhiteSpace(address.State) ? string.Empty : ", " + address.State)} {address.PostalCode}");
            addressColumn.Item().Text(address.Country);
            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                addressColumn.Item().PaddingTop(4).Text(customerEmail).FontSize(9).FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private static void ComposeLineTable(IContainer linesContainer, Invoice invoice)
    {
        linesContainer.Table(tableDescriptor =>
        {
            tableDescriptor.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            tableDescriptor.Header(headerRow =>
            {
                static IContainer Style(IContainer headerCell)
                    => headerCell.DefaultTextStyle(textStyle => textStyle.SemiBold())
                                 .PaddingVertical(6)
                                 .BorderBottom(1)
                                 .BorderColor(Colors.Grey.Darken1)
                                 .Background(Colors.Grey.Lighten3);

                headerRow.Cell().Element(Style).Text("#");
                headerRow.Cell().Element(Style).Text("Description");
                headerRow.Cell().Element(Style).AlignRight().Text("Qty");
                headerRow.Cell().Element(Style).AlignRight().Text("Unit price");
                headerRow.Cell().Element(Style).AlignRight().Text("Discount");
                headerRow.Cell().Element(Style).AlignRight().Text("Line total");
            });

            foreach (var line in invoice.Lines)
            {
                tableDescriptor.Cell().Element(cellBody).Text(line.LineNumber.ToString(InvoiceCulture));
                tableDescriptor.Cell().Element(cellBody).Column(productColumn =>
                {
                    productColumn.Item().Text(line.ProductName);
                    productColumn.Item().Text($"SKU {line.ProductSku}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                tableDescriptor.Cell().Element(cellBody).AlignRight().Text(line.Quantity.ToString(InvoiceCulture));
                tableDescriptor.Cell().Element(cellBody).AlignRight().Text(FormatMoney(line.UnitPrice, invoice.CurrencyCode));
                tableDescriptor.Cell().Element(cellBody).AlignRight().Text(FormatMoney(-line.DiscountAmount, invoice.CurrencyCode));
                tableDescriptor.Cell().Element(cellBody).AlignRight().Text(FormatMoney(line.LineTotal, invoice.CurrencyCode));
            }

            static IContainer cellBody(IContainer cell)
                => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6);
        });
    }

    private static void AppendTotalsRow(ColumnDescriptor column, string label, decimal value, string currencyCode)
    {
        column.Item().Row(row =>
        {
            row.RelativeItem().AlignRight().Text(label);
            row.ConstantItem(110).AlignRight().Text(FormatMoney(value, currencyCode));
        });
    }

    private static void ComposeFooter(IContainer footerContainer, Invoice invoice)
    {
        footerContainer.AlignCenter().Text(textComposer =>
        {
            textComposer.Span($"FreshCart · {invoice.InvoiceNumber} · ").FontSize(8).FontColor(Colors.Grey.Darken1);
            textComposer.Span("Generated ").FontSize(8).FontColor(Colors.Grey.Darken1);
            textComposer.Span(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", InvoiceCulture)).FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private static string FormatMoney(decimal amount, string currencyCode)
        => string.Create(InvoiceCulture, $"{currencyCode} {amount,12:N2}");

    private static string InvoiceTitleFor(InvoiceKind kind) => kind switch
    {
        InvoiceKind.Sale       => "INVOICE",
        InvoiceKind.CreditNote => "CREDIT NOTE",
        InvoiceKind.ProForma   => "PRO-FORMA INVOICE",
        _ => "INVOICE",
    };
}
