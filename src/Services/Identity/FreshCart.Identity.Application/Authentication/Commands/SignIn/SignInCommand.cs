using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Identity.Application.Authentication.Commands.SignIn;

/// <summary>
/// Authenticates a user by email + password and, if MFA is enabled, by the supplied six-digit code.
/// When <see cref="UseCookie"/> is true the API endpoint signs the user in with the cookie scheme;
/// otherwise the endpoint replies with JWT access + refresh tokens (mobile / service-to-service).
/// </summary>
public sealed record SignInCommand(
    string Email,
    string Password,
    string? MultiFactorCode,
    bool UseCookie,
    bool RememberMe) : ICommand<SignInResult>;
