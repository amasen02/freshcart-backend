using FreshCart.CustomerSupport.Api.Domain;
using FreshCart.CustomerSupport.Api.Realtime;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Xunit;

namespace FreshCart.CustomerSupport.Tests.Realtime;

public sealed class SignalRSupportChatNotifierTests
{
    private static readonly Guid RecipientId = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    private static readonly Guid SessionId = Guid.Parse("b0000000-0000-0000-0000-000000000002");

    private readonly IClientProxy _recipientProxy = Substitute.For<IClientProxy>();
    private readonly IHubContext<SupportChatHub> _hubContext = Substitute.For<IHubContext<SupportChatHub>>();
    private readonly SignalRSupportChatNotifier _notifier;

    public SignalRSupportChatNotifierTests()
    {
        var clients = Substitute.For<IHubClients>();
        clients.Group(SupportGroupNames.ForUser(RecipientId)).Returns(_recipientProxy);
        _hubContext.Clients.Returns(clients);

        _notifier = new SignalRSupportChatNotifier(_hubContext);
    }

    [Fact]
    public async Task RelayingToARecipientDoesNotPropagateTheSenderCancellationToken()
    {
        using var alreadyCancelled = new CancellationTokenSource();
        await alreadyCancelled.CancelAsync();

        var session = new ChatSessionDto(
            SessionId.ToString(),
            "Where is my order?",
            Guid.NewGuid().ToString(),
            "Sam Shopper",
            null,
            null,
            nameof(SessionStatus.Queued),
            DateTimeOffset.UnixEpoch);

        await _notifier.ChatAssignedAsync(RecipientId, session, alreadyCancelled.Token);

        await _recipientProxy.Received(1).SendCoreAsync(
            SupportHubMethodNames.ChatAssigned,
            Arg.Any<object?[]>(),
            CancellationToken.None);
    }
}
