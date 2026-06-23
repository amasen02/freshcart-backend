using System.Security.Claims;
using FreshCart.CustomerSupport.Api.Assignment;
using FreshCart.CustomerSupport.Api.Authentication;
using FreshCart.CustomerSupport.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FreshCart.CustomerSupport.Api.Realtime;

/// <summary>
/// Live support endpoint. The hub is deliberately thin: it owns only connection-scoped concerns
/// (group membership, agent registration) and forwards every conversation decision to the
/// <see cref="ChatSessionCoordinator"/>, which is where the testable rules live.
/// </summary>
[Authorize]
public sealed class SupportChatHub : Hub
{
    public const string HubPath = "/hubs/support";

    private readonly ChatSessionCoordinator _coordinator;
    private readonly IAgentAvailabilityRegistry _availabilityRegistry;

    public SupportChatHub(ChatSessionCoordinator coordinator, IAgentAvailabilityRegistry availabilityRegistry)
    {
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(availabilityRegistry);

        _coordinator = coordinator;
        _availabilityRegistry = availabilityRegistry;
    }

    public override async Task OnConnectedAsync()
    {
        var userPrincipal = Context.User
            ?? throw new HubException("The connection is not authenticated.");

        var userId = userPrincipal.GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, SupportGroupNames.ForUser(userId)).ConfigureAwait(false);

        if (userPrincipal.IsSupportAgent())
        {
            await _availabilityRegistry
                .RegisterAsync(userId, userPrincipal.GetDisplayName(), Context.ConnectionAborted)
                .ConfigureAwait(false);

            // A newly available agent should immediately pick up whoever has been waiting longest.
            await _coordinator.DrainQueueAsync(Context.ConnectionAborted).ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userPrincipal = Context.User;
        if (userPrincipal is not null && userPrincipal.IsSupportAgent())
        {
            var agentId = userPrincipal.GetUserId();

            // A disconnecting customer keeps their place: the session stays open and reconnecting
            // resumes it from REST history, so only an agent drop needs to re-queue its sessions.
            await _coordinator.HandleAgentDisconnectedAsync(agentId, CancellationToken.None).ConfigureAwait(false);
            await _availabilityRegistry.DeregisterAsync(agentId, CancellationToken.None).ConfigureAwait(false);
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    public Task<ChatSessionDto> RequestChat(string topic)
    {
        var userPrincipal = RequireUser();
        if (userPrincipal.IsSupportAgent())
        {
            throw new HubException("Support agents cannot open customer chat sessions.");
        }

        return _coordinator.RequestChatAsync(
            userPrincipal.GetUserId(),
            userPrincipal.GetDisplayName(),
            topic,
            Context.ConnectionAborted);
    }

    public Task SendMessage(Guid sessionId, string text)
    {
        var userPrincipal = RequireUser();
        var senderRole = userPrincipal.IsSupportAgent() ? SenderRole.Agent : SenderRole.Customer;

        return _coordinator.SendMessageAsync(
            userPrincipal.GetUserId(),
            userPrincipal.GetDisplayName(),
            senderRole,
            sessionId,
            text,
            Context.ConnectionAborted);
    }

    public Task SetTyping(Guid sessionId, bool isTyping)
    {
        var userPrincipal = RequireUser();

        return _coordinator.RelayTypingAsync(
            userPrincipal.GetUserId(),
            userPrincipal.GetDisplayName(),
            sessionId,
            isTyping,
            Context.ConnectionAborted);
    }

    public Task EndChat(Guid sessionId)
    {
        var userPrincipal = RequireUser();

        return _coordinator.EndChatAsync(userPrincipal.GetUserId(), sessionId, Context.ConnectionAborted);
    }

    private ClaimsPrincipal RequireUser() =>
        Context.User ?? throw new HubException("The connection is not authenticated.");
}
