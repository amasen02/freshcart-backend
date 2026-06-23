using Carter;
using FreshCart.Reporting.Api.Authentication;
using FreshCart.Reporting.Application.Common.Models;
using FreshCart.Reporting.Application.Exports.Commands.ExportSalesTransactions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FreshCart.Reporting.Api.Endpoints.Exports;

public sealed class ExportEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var exportsGroup = app
            .MapGroup("/exports")
            .RequireAuthorization(AuthorizationPolicies.BackOfficeUser)
            .WithTags("Exports");

        exportsGroup.MapGet("/sales-transactions.xlsx", ExportSalesTransactionsAsync)
            .WithSummary("Download the daily-bucketed sales transactions for the period as an Excel workbook.")
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<IResult> ExportSalesTransactionsAsync(
        ISender mediator,
        PeriodPreset? preset,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var command = new ExportSalesTransactionsCommand(new PeriodSelector(preset, fromUtc, toUtc));
        var commandResult = await mediator.Send(command, cancellationToken).ConfigureAwait(false);

        return Results.File(
            commandResult.Content,
            contentType: commandResult.ContentType,
            fileDownloadName: commandResult.FileName);
    }
}
