using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Identity.Application.Account.Commands.VerifyMultiFactorEnrollment;

/// <summary>
/// Completes authenticator enrollment by verifying a TOTP code produced from the shared key issued by
/// <c>EnrollMultiFactorCommand</c>. On success multi-factor sign-in becomes mandatory for the account.
/// </summary>
public sealed record VerifyMultiFactorEnrollmentCommand(
    Guid UserId,
    string VerificationCode) : ICommand<VerifyMultiFactorEnrollmentResult>;
