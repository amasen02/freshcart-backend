using System.Security.Claims;
using FreshCart.Identity.Application.Common.Abstractions;
using Microsoft.AspNetCore.Http;

namespace FreshCart.Identity.Api.Configuration;

/// <summary>
/// Adapter that translates the in-flight <see cref="HttpContext"/> into the abstract surface required
/// by the application layer. Lives in the API project so the Application project does not need a
/// hard reference to ASP.NET Core types.
/// </summary>
public sealed class HttpContextCurrentRequestContext(IHttpContextAccessor httpContextAccessor)
    : ICurrentRequestContext
{
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? CorrelationId => httpContextAccessor.HttpContext?.TraceIdentifier;

    public Guid? AuthenticatedUserId
    {
        get
        {
            var subjectClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(subjectClaim, out var parsedUserId) ? parsedUserId : null;
        }
    }
}
