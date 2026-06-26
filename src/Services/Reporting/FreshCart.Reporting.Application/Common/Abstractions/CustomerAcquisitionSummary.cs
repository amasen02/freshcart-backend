namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Acquisition and retention headline numbers for a reporting period.
/// </summary>
public sealed record CustomerAcquisitionSummary(
    long NewCustomers,
    long ReturningCustomers,
    long ChurnedCustomers,
    decimal AverageLifetimeValue);
