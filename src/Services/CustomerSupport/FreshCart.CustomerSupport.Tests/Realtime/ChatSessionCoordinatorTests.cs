using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.CustomerSupport.Api.Domain;
using FreshCart.CustomerSupport.Api.Realtime;
using FreshCart.CustomerSupport.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FreshCart.CustomerSupport.Tests.Realtime;

public sealed class ChatSessionCoordinatorTests
{
    private static readonly Guid CustomerId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherCustomerId = Guid.Parse("c0000000-0000-0000-0000-000000000002");
    private static readonly Guid AgentId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid IntruderId = Guid.Parse("d0000000-0000-0000-0000-000000000009");
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 9, 30, 0, TimeSpan.Zero);

    private const string CustomerName = "Dana Customer";
    private const string AgentName = "Ravi Agent";
    private const string Topic = "Where is my order?";

    private readonly InMemoryChatSessionRepository _sessions = new();
    private readonly InMemoryChatMessageRepository _messages = new();
    private readonly QueueingAgentAssignmentStrategy _assignmentStrategy = new();
    private readonly StubAgentAvailabilityRegistry _availabilityRegistry = new();
    private readonly InMemoryChatWaitingLine _waitingLine = new();
    private readonly ISupportChatNotifier _notifier = Substitute.For<ISupportChatNotifier>();
    private readonly ChatSessionCoordinator _coordinator;

    public ChatSessionCoordinatorTests()
    {
        _coordinator = new ChatSessionCoordinator(
            _sessions,
            _messages,
            _assignmentStrategy,
            _availabilityRegistry,
            _waitingLine,
            _notifier,
            new FixedTimeProvider(Now),
            NullLogger<ChatSessionCoordinator>.Instance);
    }

    [Fact]
    public async Task RequestingAChatWithAFreeAgentAssignsItAndNotifiesBothParties()
    {
        MakeAgentAvailable();

        var session = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);

        session.Status.Should().Be(nameof(SessionStatus.Active));
        session.AgentId.Should().Be(AgentId.ToString());
        session.AgentDisplayName.Should().Be(AgentName);

        await _notifier.Received(1).ChatAssignedAsync(
            CustomerId, Arg.Is<ChatSessionDto>(dto => dto.SessionId == session.SessionId), CancellationToken.None);
        await _notifier.Received(1).ChatAssignedAsync(
            AgentId, Arg.Is<ChatSessionDto>(dto => dto.SessionId == session.SessionId), CancellationToken.None);
    }

    [Fact]
    public async Task RequestingAChatWithNoAgentQueuesItAndReportsTheFirstPosition()
    {
        var session = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);

        session.Status.Should().Be(nameof(SessionStatus.Queued));
        session.AgentId.Should().BeNull();

        var snapshot = await _waitingLine.SnapshotAsync(CancellationToken.None);
        snapshot.Should().ContainSingle().Which.Should().Be(Guid.Parse(session.SessionId));

        await _notifier.Received(1).QueuePositionChangedAsync(
            CustomerId, Guid.Parse(session.SessionId), 1, CancellationToken.None);
    }

    [Fact]
    public async Task ACustomerCannotOpenASecondChatWhileOneIsStillOpen()
    {
        await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);

        var openSecond = () => _coordinator.RequestChatAsync(CustomerId, CustomerName, "Another issue", CancellationToken.None);

        (await openSecond.Should().ThrowAsync<BadRequestException>())
            .Which.Message.Should().Contain("already have an open support chat");
    }

    [Fact]
    public Task ABlankTopicIsRejected()
    {
        var requestWithBlankTopic = () =>
            _coordinator.RequestChatAsync(CustomerId, CustomerName, "   ", CancellationToken.None);

        return requestWithBlankTopic.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task SendingAMessageAsAParticipantPersistsItAndRelaysToBothParties()
    {
        MakeAgentAvailable();
        var session = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);
        var sessionId = Guid.Parse(session.SessionId);
        _notifier.ClearReceivedCalls();

        await _coordinator.SendMessageAsync(
            CustomerId, CustomerName, SenderRole.Customer, sessionId, "  Hello there  ", CancellationToken.None);

        _messages.StoredMessages.Should().ContainSingle();
        _messages.StoredMessages[0].Text.Should().Be("Hello there", "leading and trailing whitespace is trimmed");

        await _notifier.Received(1).MessageReceivedAsync(
            CustomerId, Arg.Any<ChatMessageDto>(), CancellationToken.None);
        await _notifier.Received(1).MessageReceivedAsync(
            AgentId, Arg.Any<ChatMessageDto>(), CancellationToken.None);
    }

    [Fact]
    public async Task SendingAMessageToASessionYouAreNotPartOfIsRejected()
    {
        MakeAgentAvailable();
        var session = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);
        var sessionId = Guid.Parse(session.SessionId);

        var sendAsIntruder = () => _coordinator.SendMessageAsync(
            IntruderId, "Intruder", SenderRole.Customer, sessionId, "Let me in", CancellationToken.None);

        (await sendAsIntruder.Should().ThrowAsync<ForbiddenException>())
            .Which.Message.Should().Be("Sender is not part of this chat session.");

        _messages.StoredMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task AnEmptyMessageIsRejected()
    {
        MakeAgentAvailable();
        var session = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);
        var sessionId = Guid.Parse(session.SessionId);

        var sendBlank = () => _coordinator.SendMessageAsync(
            CustomerId, CustomerName, SenderRole.Customer, sessionId, "   ", CancellationToken.None);

        await sendBlank.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task TypingIsRelayedOnlyToTheOtherParticipant()
    {
        MakeAgentAvailable();
        var session = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);
        var sessionId = Guid.Parse(session.SessionId);

        await _coordinator.RelayTypingAsync(CustomerId, CustomerName, sessionId, isTyping: true, CancellationToken.None);

        await _notifier.Received(1).ParticipantTypingAsync(
            AgentId, sessionId, CustomerName, true, CancellationToken.None);
        await _notifier.DidNotReceive().ParticipantTypingAsync(
            CustomerId, Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndingAChatReleasesTheAgentAndDrainsTheNextQueuedCustomerInFifoOrder()
    {
        MakeAgentAvailable();
        var firstSession = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);

        // The agent is now busy, so the next two customers wait in arrival order.
        var secondSession = await _coordinator.RequestChatAsync(
            OtherCustomerId, "Second customer", "Refund", CancellationToken.None);
        var thirdCustomerId = Guid.Parse("c0000000-0000-0000-0000-000000000003");
        var thirdSession = await _coordinator.RequestChatAsync(
            thirdCustomerId, "Third customer", "Damaged item", CancellationToken.None);
        _notifier.ClearReceivedCalls();

        await _coordinator.EndChatAsync(CustomerId, Guid.Parse(firstSession.SessionId), CancellationToken.None);

        _assignmentStrategy.ReleasedAgents.Should().ContainSingle().Which.Should().Be(AgentId);

        var secondSessionId = Guid.Parse(secondSession.SessionId);
        var reassignedSecond = await _sessions.GetByIdAsync(secondSessionId, CancellationToken.None);
        reassignedSecond!.Status.Should().Be(SessionStatus.Active, "the customer who waited longest is served first");
        reassignedSecond.AgentId.Should().Be(AgentId);

        var thirdSessionId = Guid.Parse(thirdSession.SessionId);
        var stillQueuedThird = await _sessions.GetByIdAsync(thirdSessionId, CancellationToken.None);
        stillQueuedThird!.Status.Should().Be(SessionStatus.Queued);

        await _notifier.Received(1).ChatAssignedAsync(
            OtherCustomerId, Arg.Any<ChatSessionDto>(), CancellationToken.None);
        await _notifier.Received(1).QueuePositionChangedAsync(
            thirdCustomerId, thirdSessionId, 1, CancellationToken.None);
    }

    [Fact]
    public async Task OnlyAParticipantCanEndTheChat()
    {
        MakeAgentAvailable();
        var session = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);

        var endAsIntruder = () => _coordinator.EndChatAsync(
            IntruderId, Guid.Parse(session.SessionId), CancellationToken.None);

        await endAsIntruder.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task EndingAlreadyEndedChatIsRejected()
    {
        MakeAgentAvailable();
        var session = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);
        var sessionId = Guid.Parse(session.SessionId);
        await _coordinator.EndChatAsync(CustomerId, sessionId, CancellationToken.None);

        var endAgain = () => _coordinator.EndChatAsync(CustomerId, sessionId, CancellationToken.None);

        await endAgain.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task WhenAnAgentDisconnectsTheirActiveChatsReturnToTheQueueAndCustomersAreNotified()
    {
        MakeAgentAvailable();
        var session = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);
        var sessionId = Guid.Parse(session.SessionId);
        _notifier.ClearReceivedCalls();

        await _coordinator.HandleAgentDisconnectedAsync(AgentId, CancellationToken.None);

        var requeued = await _sessions.GetByIdAsync(sessionId, CancellationToken.None);
        requeued!.Status.Should().Be(SessionStatus.Queued);
        requeued.AgentId.Should().BeNull();

        var snapshot = await _waitingLine.SnapshotAsync(CancellationToken.None);
        snapshot.Should().Contain(sessionId);

        await _notifier.Received(1).QueuePositionChangedAsync(
            CustomerId, sessionId, 1, CancellationToken.None);
    }

    [Fact]
    public async Task DrainingTheQueueWithNoFreeAgentLeavesTheCustomerInTheLine()
    {
        var session = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);
        var sessionId = Guid.Parse(session.SessionId);

        await _coordinator.DrainQueueAsync(CancellationToken.None);

        var snapshot = await _waitingLine.SnapshotAsync(CancellationToken.None);
        snapshot.Should().ContainSingle().Which.Should().Be(sessionId);

        var stillQueued = await _sessions.GetByIdAsync(sessionId, CancellationToken.None);
        stillQueued!.Status.Should().Be(SessionStatus.Queued);
    }

    [Fact]
    public async Task DrainingWithNoFreeAgentKeepsTheHeadCustomerAtTheFrontOfTheLine()
    {
        var headSession = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);
        await _coordinator.RequestChatAsync(OtherCustomerId, "Second customer", "Refund", CancellationToken.None);
        var headSessionId = Guid.Parse(headSession.SessionId);

        await _coordinator.DrainQueueAsync(CancellationToken.None);

        var snapshot = await _waitingLine.SnapshotAsync(CancellationToken.None);
        snapshot[0].Should().Be(headSessionId, "a failed drain must not cost the first-in-line customer their place");
    }

    [Fact]
    public async Task ADrainWhosePersistenceFailsRestoresTheSessionToTheFrontAndReleasesTheAgent()
    {
        var headSession = await _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);
        await _coordinator.RequestChatAsync(OtherCustomerId, "Second customer", "Refund", CancellationToken.None);
        var headSessionId = Guid.Parse(headSession.SessionId);
        MakeAgentAvailable();
        _sessions.FailNextSave();

        var drain = () => _coordinator.DrainQueueAsync(CancellationToken.None);

        await drain.Should().ThrowAsync<InvalidOperationException>();
        var snapshot = await _waitingLine.SnapshotAsync(CancellationToken.None);
        snapshot[0].Should().Be(headSessionId, "the dropped assignment must not lose the customer their place");
        _assignmentStrategy.ReleasedAgents.Should().Contain(AgentId, "the agent charge must be undone when the assignment never persisted");
    }

    [Fact]
    public async Task ARequestWhoseAssignmentPersistenceFailsReleasesTheChargedAgent()
    {
        MakeAgentAvailable();
        _sessions.FailNextSave();

        var request = () => _coordinator.RequestChatAsync(CustomerId, CustomerName, Topic, CancellationToken.None);

        await request.Should().ThrowAsync<InvalidOperationException>();
        _assignmentStrategy.ReleasedAgents.Should().ContainSingle().Which.Should().Be(AgentId);
    }

    private void MakeAgentAvailable()
    {
        _availabilityRegistry.SetDisplayName(AgentId, AgentName);
        _assignmentStrategy.MakeAvailable(AgentId);
    }
}
