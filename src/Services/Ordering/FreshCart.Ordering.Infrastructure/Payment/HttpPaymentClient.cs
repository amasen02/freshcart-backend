using System.Net.Http.Json;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Ordering.Application.Abstractions;

namespace FreshCart.Ordering.Infrastructure.Payment;

/// <summary>
/// Adapter from the <see cref="IPaymentClient"/> port to the Payment REST API. The order id is sent
/// as the idempotency key so a retried capture never double-charges the customer. A declined card is
/// a business result; a malformed or missing response body is a contract fault and throws so the
/// saga's retry policy can react.
/// </summary>
public sealed class HttpPaymentClient(HttpClient httpClient) : IPaymentClient
{
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";

    private const string CapturePath = "/payments";

    public async Task<PaymentCaptureResult> CapturePaymentAsync(
        PaymentCaptureRequest captureRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureRequest);

        var payload = new PaymentCaptureRequestPayload(
            captureRequest.OrderId,
            captureRequest.CustomerId,
            captureRequest.Amount,
            captureRequest.CurrencyCode,
            captureRequest.PaymentMethod);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, CapturePath)
        {
            Content = JsonContent.Create(payload),
        };
        requestMessage.Headers.Add(IdempotencyKeyHeaderName, captureRequest.OrderId.ToString());

        using var responseMessage = await httpClient
            .SendAsync(requestMessage, cancellationToken)
            .ConfigureAwait(false);

        responseMessage.EnsureSuccessStatusCode();

        var response = await responseMessage.Content
            .ReadFromJsonAsync<PaymentCaptureResponsePayload>(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InternalServerException(
                $"Payment service returned an empty body when capturing order {captureRequest.OrderId}.");

        return new PaymentCaptureResult(response.Succeeded, response.PaymentId, response.FailureReason);
    }

    public async Task<PaymentRefundResult> RefundPaymentAsync(
        PaymentRefundRequest refundRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(refundRequest);

        var refundPath = $"/payments/{refundRequest.PaymentId}/refunds";

        var payload = new PaymentRefundRequestPayload(refundRequest.Amount, refundRequest.Reason);

        using var responseMessage = await httpClient
            .PostAsJsonAsync(refundPath, payload, cancellationToken)
            .ConfigureAwait(false);

        responseMessage.EnsureSuccessStatusCode();

        var response = await responseMessage.Content
            .ReadFromJsonAsync<PaymentRefundResponsePayload>(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InternalServerException(
                $"Payment service returned an empty body when refunding payment {refundRequest.PaymentId}.");

        return new PaymentRefundResult(response.Succeeded, response.RefundedAmount, response.FailureReason);
    }
}
