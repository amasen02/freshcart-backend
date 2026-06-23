using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Identity.Application.Authentication.Commands.SignUp;

/// <summary>
/// Creates a new <c>Customer</c> account. The new account is granted the <c>Customer</c> role and is
/// signed in immediately if <see cref="SignInImmediately"/> is true. The caller (typically the
/// gateway) decides whether to sign in immediately based on UX policy.
/// </summary>
public sealed record SignUpCommand(
    string Email,
    string Password,
    string DisplayName,
    bool MarketingConsent,
    bool SignInImmediately) : ICommand<SignUpResult>;
