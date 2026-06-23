using System.Security.Claims;
using FreshCart.Gateway.Yarp.Middleware;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace FreshCart.Gateway.Tests.Middleware;

public sealed class AntiforgeryValidationMiddlewareTests
{
    [Fact]
    public async Task CookieAuthenticatedUnsafeRequestWithInvalidTokenIsRejectedWithBadRequest()
    {
        var antiforgery = Substitute.For<IAntiforgery>();
        antiforgery.IsRequestValidAsync(Arg.Any<HttpContext>()).Returns(false);

        var nextInvoked = false;
        var middleware = new AntiforgeryValidationMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        }, antiforgery);

        var httpContext = CreateAuthenticatedContext(HttpMethods.Post, "/api/orders");

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        nextInvoked.Should().BeFalse();
    }

    [Fact]
    public async Task CookieAuthenticatedUnsafeRequestWithValidTokenIsForwarded()
    {
        var antiforgery = Substitute.For<IAntiforgery>();
        antiforgery.IsRequestValidAsync(Arg.Any<HttpContext>()).Returns(true);

        var nextInvoked = false;
        var middleware = new AntiforgeryValidationMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        }, antiforgery);

        var httpContext = CreateAuthenticatedContext(HttpMethods.Post, "/api/orders");

        await middleware.InvokeAsync(httpContext);

        nextInvoked.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task BearerAuthenticatedUnsafeRequestSkipsAntiforgeryAndIsForwarded()
    {
        var antiforgery = Substitute.For<IAntiforgery>();

        var nextInvoked = false;
        var middleware = new AntiforgeryValidationMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        }, antiforgery);

        var httpContext = CreateAuthenticatedContext(HttpMethods.Post, "/api/orders");
        httpContext.Request.Headers.Authorization = "Bearer downstream.jwt.token";

        await middleware.InvokeAsync(httpContext);

        nextInvoked.Should().BeTrue();
        await antiforgery.DidNotReceive().IsRequestValidAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task SafeMethodSkipsAntiforgeryAndIsForwarded()
    {
        var antiforgery = Substitute.For<IAntiforgery>();

        var nextInvoked = false;
        var middleware = new AntiforgeryValidationMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        }, antiforgery);

        var httpContext = CreateAuthenticatedContext(HttpMethods.Get, "/api/orders");

        await middleware.InvokeAsync(httpContext);

        nextInvoked.Should().BeTrue();
        await antiforgery.DidNotReceive().IsRequestValidAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task UnsafeRequestOutsideTheApiPrefixSkipsAntiforgeryAndIsForwarded()
    {
        var antiforgery = Substitute.For<IAntiforgery>();

        var nextInvoked = false;
        var middleware = new AntiforgeryValidationMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        }, antiforgery);

        var httpContext = CreateAuthenticatedContext(HttpMethods.Post, "/hubs/notifications");

        await middleware.InvokeAsync(httpContext);

        nextInvoked.Should().BeTrue();
        await antiforgery.DidNotReceive().IsRequestValidAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task AnonymousUnsafeRequestSkipsAntiforgeryAndIsForwarded()
    {
        var antiforgery = Substitute.For<IAntiforgery>();

        var nextInvoked = false;
        var middleware = new AntiforgeryValidationMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        }, antiforgery);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/auth/sign-in";

        await middleware.InvokeAsync(httpContext);

        nextInvoked.Should().BeTrue();
        await antiforgery.DidNotReceive().IsRequestValidAsync(Arg.Any<HttpContext>());
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string method, string path)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            CookieAuthenticationDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
        };
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }
}
