using System.Globalization;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Application.Common.Models;
using FreshCart.Reporting.Domain.Sales;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreshCart.Reporting.Infrastructure.Scheduled;

/// <summary>
/// Scheduled job that builds the previous day's sales-summary Excel and stores it in Blob Storage
/// at <c>scheduled-reports/daily/&lt;yyyy-MM-dd&gt;.xlsx</c>. Runs once an hour and is idempotent:
/// if the file for yesterday is already present it skips work.
/// </summary>
public sealed class DailySalesReportBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    ILogger<DailySalesReportBackgroundService> logger) : BackgroundService
{
    private const string ContainerName = "scheduled-reports";

    private const string DayBucketFormat = "yyyy-MM-dd";

    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await BuildYesterdayReportAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception buildFailure)
            {
                logger.LogError(buildFailure, "Daily sales report build failed; will retry next cycle.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private async Task BuildYesterdayReportAsync(CancellationToken cancellationToken)
    {
        using var serviceScope = serviceScopeFactory.CreateScope();
        var salesWarehouse = serviceScope.ServiceProvider.GetRequiredService<ISalesReadWarehouse>();
        var excelExporter = serviceScope.ServiceProvider.GetRequiredService<IExcelExporter>();
        var documentStore = serviceScope.ServiceProvider.GetRequiredService<IDocumentStore>();

        var nowUtc = timeProvider.GetUtcNow();
        var startOfYesterday = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, TimeSpan.Zero).AddDays(-1);
        var endOfYesterday = startOfYesterday.AddDays(1);
        var yesterdayBucket = startOfYesterday.UtcDateTime.ToString(DayBucketFormat, CultureInfo.InvariantCulture);
        var blobName = $"daily/{yesterdayBucket}.xlsx";

        if (await documentStore.ExistsAsync(ContainerName, blobName, cancellationToken).ConfigureAwait(false))
        {
            logger.LogDebug("Daily sales report for {Day} already exists; skipping.", yesterdayBucket);
            return;
        }

        var period = new ReportingPeriod(startOfYesterday, endOfYesterday);
        var dailyPoints = await salesWarehouse
            .GetTimeSeriesAsync(period, AggregationBucket.Hourly, cancellationToken)
            .ConfigureAwait(false);

        var rows = dailyPoints.Select(snapshot => new
        {
            HourBucketUtc = snapshot.Day,
            snapshot.OrderCount,
            snapshot.GrossRevenue,
            snapshot.DiscountTotal,
            snapshot.RefundTotal,
            snapshot.TaxTotal,
            snapshot.NetRevenue,
            snapshot.AverageOrderValue,
        });

        var rendered = await excelExporter
            .ExportTabularAsync(
                sheetName: $"Daily sales {yesterdayBucket}",
                rows: rows,
                options: new ExcelExportOptions(
                    Title: $"FreshCart daily sales {yesterdayBucket}",
                    Author: "FreshCart Reporting"),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await documentStore
            .StoreAsync(ContainerName, blobName, rendered.Content, rendered.ContentType, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Stored daily sales report for {Day} ({ByteSize} bytes).", yesterdayBucket, rendered.Content.Length);
    }
}
