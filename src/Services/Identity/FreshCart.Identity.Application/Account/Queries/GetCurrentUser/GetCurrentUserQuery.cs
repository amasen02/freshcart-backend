using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Identity.Application.Common.Models;

namespace FreshCart.Identity.Application.Account.Queries.GetCurrentUser;

/// <summary>
/// Returns the currently authenticated user's <see cref="AuthenticationProfile"/>. Called by the
/// Angular shell on bootstrap to hydrate the session-state signal store.
/// </summary>
public sealed record GetCurrentUserQuery(Guid UserId) : IQuery<AuthenticationProfile>;
