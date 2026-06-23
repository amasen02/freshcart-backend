using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Exceptions.Handler;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FreshCart.BuildingBlocks.Tests.Exceptions;

public sealed class CustomExceptionHandlerTests
{
    [Theory]
    [InlineData(typeof(NotFoundException),     "Customer not found",  HttpStatusCode.NotFound)]
    [InlineData(typeof(BadRequestException),   "Bad input",           HttpStatusCode.BadRequest)]
    [InlineData(typeof(ConflictException),     "Already exists",      HttpStatusCode.Conflict)]
    [InlineData(typeof(ForbiddenException),    "Not allowed",         HttpStatusCode.Forbidden)]
    [InlineData(typeof(DomainException),       "Invariant violated",  HttpStatusCode.UnprocessableEntity)]
    [InlineData(typeof(InternalServerException), "Unexpected",        HttpStatusCode.InternalServerError)]
    public async Task TryHandleMapsKnownExceptionsToTheExpectedStatusCode(Type exceptionType, string message, HttpStatusCode expected)
    {
        var handler = new CustomExceptionHandler(Substitute.For<ILogger<CustomExceptionHandler>>());
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be((int)expected);
    }

    [Fact]
    public async Task TryHandleMapsValidationExceptionToBadRequestWithValidationErrors()
    {
        var handler = new CustomExceptionHandler(Substitute.For<ILogger<CustomExceptionHandler>>());
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var exception = new ValidationException(new[]
        {
            new ValidationFailure("Email", "Email is required."),
            new ValidationFailure("Email", "Email is invalid."),
            new ValidationFailure("Password", "Password is required."),
        });

        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);

        var validationErrors = document.RootElement.GetProperty("validationErrors");
        validationErrors.GetProperty("Email").GetArrayLength().Should().Be(2);
        validationErrors.GetProperty("Password").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task TryHandleEmitsTraceIdExtensionMatchingTheRequestId()
    {
        var handler = new CustomExceptionHandler(Substitute.For<ILogger<CustomExceptionHandler>>());
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() }, TraceIdentifier = "trace-1234" };

        await handler.TryHandleAsync(httpContext, new NotFoundException("missing"), CancellationToken.None);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);

        document.RootElement.GetProperty("traceId").GetString().Should().Be("trace-1234");
    }
}
