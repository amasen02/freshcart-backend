namespace FreshCart.Identity.Application.Account.Commands.EnrollMultiFactor;

/// <summary>
/// The new shared secret in two shapes: <see cref="SharedKey"/> grouped for manual entry and
/// <see cref="AuthenticatorUri"/> for QR-code rendering by authenticator apps.
/// </summary>
public sealed record EnrollMultiFactorResult(string SharedKey, Uri AuthenticatorUri);
