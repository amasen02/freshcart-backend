namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Acquisition and retention headline numbers for a reporting period.
/// </summary>
public sealed record CustomerAcquisitionSummary(
    int NewCustomers,
    int ReturningCustomers,
    int ChurnedCustomers,
    decimal AverageLifetimeValue);
