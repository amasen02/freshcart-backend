namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Revenue contribution of one payment method within a reporting period.
/// </summary>
public sealed record RevenueByPaymentMethodRow(string PaymentMethod, int TransactionCount, decimal NetRevenue);
