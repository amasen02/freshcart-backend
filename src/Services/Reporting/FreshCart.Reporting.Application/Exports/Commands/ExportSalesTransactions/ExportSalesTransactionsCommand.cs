using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Models;

namespace FreshCart.Reporting.Application.Exports.Commands.ExportSalesTransactions;

/// <summary>
/// Renders the daily sales time series of the selected period as a downloadable Excel workbook.
/// </summary>
public sealed record ExportSalesTransactionsCommand(PeriodSelector Period)
    : ICommand<ExportSalesTransactionsResult>;
