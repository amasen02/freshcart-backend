using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Identity.Application.Account.Commands.DisableMultiFactor;

/// <summary>
/// Disables multi-factor authentication. A current TOTP code is demanded as proof of possession so a
/// hijacked session alone cannot strip the account of its second factor.
/// </summary>
public sealed record DisableMultiFactorCommand(
    Guid UserId,
    string VerificationCode) : ICommand;
