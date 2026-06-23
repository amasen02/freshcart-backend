namespace FreshCart.Identity.Application.Account.Commands.VerifyMultiFactorEnrollment;

/// <summary>
/// Recovery codes are handed out exactly once, here; they are stored hashed and can never be read
/// back, so the caller must instruct the user to save them immediately.
/// </summary>
public sealed record VerifyMultiFactorEnrollmentResult(IReadOnlyCollection<string> RecoveryCodes);
