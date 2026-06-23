namespace FreshCart.Identity.Api.Endpoints.Account;

/// <summary>
/// Wire shape returned when authenticator enrollment starts: the grouped shared key for manual
/// entry plus the otpauth URI rendered as a QR code.
/// </summary>
public sealed record EnrollMultiFactorResponse(string SharedKey, Uri AuthenticatorUri);
