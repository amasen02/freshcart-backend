namespace FreshCart.Identity.Application.Common.Abstractions;

/// <summary>
/// Read-only snapshot of the in-flight HTTP request fields the application layer needs for audit and
/// rate-limiting decisions, without taking a hard dependency on ASP.NET Core types.
/// </summary>
public interface ICurrentRequestContext
{
    string? IpAddress { get; }

    string? UserAgent { get; }

    string? CorrelationId { get; }

    Guid? AuthenticatedUserId { get; }
}
