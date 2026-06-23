using Carter;
using FreshCart.Reporting.Api.Authentication;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Application.Invoices.Commands.GenerateInvoice;
using FreshCart.Reporting.Application.Invoices.Queries.DownloadInvoice;
using FreshCart.Reporting.Domain.Invoices;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FreshCart.Reporting.Api.Endpoints.Invoices;

public sealed class InvoiceEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var invoicesGroup = app
            .MapGroup("/invoices")
            .RequireAuthorization()
            .WithTags("Invoices");

        invoicesGroup.MapPost("/", GenerateAsync)
            .RequireAuthorization(AuthorizationPolicies.BackOfficeUser)
            .WithSummary("Generate an invoice PDF for a confirmed order (idempotent).")
            .Produces<GenerateInvoiceResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        invoicesGroup.MapGet("/{invoiceNumber}", GetSignedUrlAsync)
            .WithSummary("Return a short-lived signed URL the caller can use to download the invoice PDF.")
            .Produces<DownloadInvoiceResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        invoicesGroup.MapGet("/{invoiceNumber}/content.pdf", StreamPdfAsync)
            .WithSummary("Stream the invoice PDF directly (used by the SPA when a SAS URL is not desired).")
            .Produces<FileStreamHttpResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GenerateAsync(
        GenerateInvoiceRequest generateRequest,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var command = new GenerateInvoiceCommand(
            OrderId: generateRequest.OrderId,
            Kind: generateRequest.Kind,
            CustomerEmail: generateRequest.CustomerEmail,
            CustomerDisplayName: generateRequest.CustomerDisplayName,
            CustomerId: generateRequest.CustomerId,
            BillingAddress: generateRequest.BillingAddress,
            ShippingAddress: generateRequest.ShippingAddress,
            Lines: generateRequest.Lines,
            DiscountTotal: generateRequest.DiscountTotal,
            TaxTotal: generateRequest.TaxTotal,
            ShippingTotal: generateRequest.ShippingTotal,
            CurrencyCode: generateRequest.CurrencyCode ?? "USD",
            OriginalInvoiceNumber: generateRequest.OriginalInvoiceNumber,
            Notes: generateRequest.Notes);

        var commandResult = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.Created($"/invoices/{commandResult.InvoiceNumber}", commandResult);
    }

    private static async Task<IResult> GetSignedUrlAsync(
        string invoiceNumber,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var queryResult = await mediator
            .Send(new DownloadInvoiceQuery(invoiceNumber), cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(queryResult);
    }

    private static async Task<IResult> StreamPdfAsync(
        string invoiceNumber,
        IDocumentStore documentStore,
        CancellationToken cancellationToken)
    {
        var contentStream = await documentStore
            .OpenReadAsync("invoices", $"{invoiceNumber}.pdf", cancellationToken)
            .ConfigureAwait(false);

        return Results.Stream(
            contentStream,
            contentType: "application/pdf",
            fileDownloadName: $"{invoiceNumber}.pdf");
    }
}
