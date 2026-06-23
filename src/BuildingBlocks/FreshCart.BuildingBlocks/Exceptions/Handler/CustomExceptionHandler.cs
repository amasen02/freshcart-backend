using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FreshCart.BuildingBlocks.Exceptions.Handler;

/// <summary>
/// Single exception sink for every FreshCart service. Maps known domain and validation
/// exceptions to RFC 7807 <see cref="ProblemDetails"/> responses; everything else becomes a
/// generic 500 whose detail stays only in the structured log.
/// </summary>
public sealed partial class CustomExceptionHandler(ILogger<CustomExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        LogUnhandledException(
            exception,
            exception.GetType().FullName,
            httpContext.Request.Method,
            httpContext.Request.Path.ToString(),
            exception.Message);

        var problemDetails = BuildProblemDetails(httpContext, exception);
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Error,
        Message = "Unhandled exception {ExceptionType} on {RequestMethod} {RequestPath}: {ExceptionMessage}")]
    private partial void LogUnhandledException(
        Exception exception,
        string? exceptionType,
        string requestMethod,
        string requestPath,
        string exceptionMessage);

    private static ProblemDetails BuildProblemDetails(HttpContext httpContext, Exception exception)
    {
        var (statusCode, title, detail) = MapStatus(exception);

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["validationErrors"] = validationException.Errors
                .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToArray(),
                    StringComparer.Ordinal);
        }

        return problemDetails;
    }

    private static (int StatusCode, string Title, string Detail) MapStatus(Exception exception) => exception switch
    {
        ValidationException         => (StatusCodes.Status400BadRequest,          "One or more validation errors occurred.",   exception.Message),
        BadRequestException bad     => (StatusCodes.Status400BadRequest,          "Bad request.",                              bad.Detail ?? bad.Message),
        NotFoundException           => (StatusCodes.Status404NotFound,            "The requested resource was not found.",     exception.Message),
        ForbiddenException          => (StatusCodes.Status403Forbidden,           "Access to the requested resource is forbidden.", exception.Message),
        ConflictException           => (StatusCodes.Status409Conflict,            "The request conflicts with the current state of the resource.", exception.Message),
        DomainException             => (StatusCodes.Status422UnprocessableEntity, "The request violates a business rule.",     exception.Message),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,        "Authentication is required.",               "The caller is not authenticated."),
        _                           => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.",             "The error has been recorded; please retry shortly."),
    };
}
