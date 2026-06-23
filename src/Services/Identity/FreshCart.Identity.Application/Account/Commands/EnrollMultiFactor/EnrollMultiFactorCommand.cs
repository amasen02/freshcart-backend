using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Identity.Application.Account.Commands.EnrollMultiFactor;

/// <summary>
/// Starts authenticator (TOTP) enrollment for the authenticated user. The authenticator key is reset
/// first so a secret left over from an abandoned enrollment can never be replayed. Multi-factor stays
/// disabled until <c>VerifyMultiFactorEnrollmentCommand</c> proves the user captured the secret.
/// </summary>
public sealed record EnrollMultiFactorCommand(Guid UserId) : ICommand<EnrollMultiFactorResult>;
