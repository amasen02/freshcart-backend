namespace FreshCart.Ordering.Application.Orders.Dtos;

public sealed record OrderSummaryDto(
    Guid OrderId,
    string Status,
    decimal GrandTotal,
    string CurrencyCode,
    int LineCount,
    DateTimeOffset SubmittedOnUtc);
