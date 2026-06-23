using System.Net.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace FreshCart.Gateway.Yarp.Middleware;

/// <summary>
/// Defence-in-depth CSRF guard at the edge. The browser authenticates with the SameSite=Strict
/// session cookie, so a cross-site forgery is already implausible; this middleware additionally
/// requires the double-submit XSRF token on every state-changing call that rides the cookie. Requests
/// that carry their own bearer token, and anonymous requests such as sign-in, skip the check: the
/// former are not cookie-driven and the latter are validated by Identity's own antiforgery once a
/// session exists.
/// </summary>
public sealed class AntiforgeryValidationMiddleware
{
    private static readonly string[] UnsafeMethods =
    [
        HttpMethod.Post.Method,
        HttpMethod.Put.Method,
        HttpMethod.Patch.Method,
        HttpMethod.Delete.Method,
    ];

    private const string ProtectedPathPrefix = "/api";

    private readonly RequestDelegate next;
    private readonly IAntiforgery antiforgery;

    public AntiforgeryValidationMiddleware(RequestDelegate next, IAntiforgery antiforgery)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(antiforgery);

        this.next = next;
        this.antiforgery = antiforgery;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (RequiresAntiforgeryValidation(httpContext))
        {
            var isValid = await antiforgery.IsRequestValidAsync(httpContext).ConfigureAwait(false);
            if (!isValid)
            {
                await WriteAntiforgeryFailureAsync(httpContext).ConfigureAwait(false);
                return;
            }
        }

        await next(httpContext).ConfigureAwait(false);
    }

    private static bool RequiresAntiforgeryValidation(HttpContext httpContext)
    {
        if (!httpContext.Request.Path.StartsWithSegments(ProtectedPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!IsUnsafeMethod(httpContext.Request.Method))
        {
            return false;
        }

        // A bearer token means a programmatic client, not a cookie session, so the double-submit
        // token does not apply. Anonymous callers (sign-in, sign-up) have no cookie session either.
        if (httpContext.Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            return false;
        }

        return httpContext.User.Identity is { IsAuthenticated: true };
    }

    private static bool IsUnsafeMethod(string method) =>
        Array.Exists(UnsafeMethods, unsafeMethod =>
            string.Equals(unsafeMethod, method, StringComparison.OrdinalIgnoreCase));

    private static Task WriteAntiforgeryFailureAsync(HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://httpstatuses.com/400",
            Title = "Anti-forgery validation failed.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "The required anti-forgery token was missing or invalid.",
            Instance = httpContext.Request.Path,
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return httpContext.Response.WriteAsJsonAsync(problemDetails);
    }
}
