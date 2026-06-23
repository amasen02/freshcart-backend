using FreshCart.BuildingBlocks.CQRS;
using MediatR;

namespace FreshCart.Identity.Application.Authentication.Commands.SignOut;

/// <summary>
/// Invalidates every active refresh token for the current user and updates the security stamp so the
/// browser cookie scheme will reject the existing session on its next request.
/// </summary>
public sealed record SignOutCommand(Guid UserId) : ICommand<Unit>;
