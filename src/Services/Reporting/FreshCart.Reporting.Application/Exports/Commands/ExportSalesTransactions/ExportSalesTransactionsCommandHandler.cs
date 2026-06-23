using System.Globalization;
using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Exports.Commands.ExportSalesTransactions;

public sealed class ExportSalesTransactionsCommandHandler(
    ISalesReadWarehouse salesWarehouse,
    IExcelExporter excelExporter,
    TimeProvider timeProvider)
    : ICommandHandler<ExportSalesTransactionsCommand, ExportSalesTransactionsResult>
{
    private const string ExportDayFormat = "yyyy-MM-dd";

    public async Task<ExportSalesTransactionsResult> Handle(
        ExportSalesTransactionsCommand command,
        CancellationToken cancellationToken)
    {
        var period = command.Period.ToPeriod(timeProvider);

        var dailyPoints = await salesWarehouse
            .GetTimeSeriesAsync(period, AggregationBucket.Daily, cancellationToken)
            .ConfigureAwait(false);

        var rows = dailyPoints.Select(snapshot => new SalesExportRow(
            Day: snapshot.Day.ToString(ExportDayFormat, CultureInfo.InvariantCulture),
            OrderCount: snapshot.OrderCount,
            GrossRevenue: snapshot.GrossRevenue,
            DiscountTotal: snapshot.DiscountTotal,
            RefundTotal: snapshot.RefundTotal,
            TaxTotal: snapshot.TaxTotal,
            ShippingTotal: snapshot.ShippingTotal,
            NetRevenue: snapshot.NetRevenue,
            AverageOrderValue: snapshot.AverageOrderValue,
            RefundRatePercent: snapshot.RefundRate * 100m));

        var rendered = await excelExporter
            .ExportTabularAsync(
                sheetName: "Sales transactions",
                rows: rows,
                options: new ExcelExportOptions(
                    Title: "FreshCart sales transactions",
                    Author: "FreshCart Reporting"),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new ExportSalesTransactionsResult(
            FileName: rendered.FileName,
            ContentType: rendered.ContentType,
            Content: rendered.Content);
    }
}
