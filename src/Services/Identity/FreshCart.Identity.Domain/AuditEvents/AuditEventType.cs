namespace FreshCart.Identity.Domain.AuditEvents;

/// <summary>
/// Canonical audit-event-type constants. Use these (never raw strings) so dashboards group correctly.
/// </summary>
public static class AuditEventType
{
    public const string SignUpSucceeded = "auth.sign-up.succeeded";

    public const string SignInSucceeded = "auth.sign-in.succeeded";

    public const string SignInFailed = "auth.sign-in.failed";

    public const string SignedOut = "auth.signed-out";

    public const string RefreshTokenIssued = "auth.refresh.issued";

    public const string RefreshTokenRevoked = "auth.refresh.revoked";

    public const string PasswordResetRequested = "account.password-reset.requested";

    public const string PasswordResetCompleted = "account.password-reset.completed";

    public const string MultiFactorEnabled = "account.mfa.enabled";

    public const string MultiFactorDisabled = "account.mfa.disabled";

    public const string MultiFactorEnrollmentStarted = "account.mfa.enrollment-started";

    public const string MultiFactorVerificationFailed = "account.mfa.verification-failed";

    public const string AccountLockedOut = "auth.account.locked-out";
}
