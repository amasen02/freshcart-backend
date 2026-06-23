using System.Globalization;
using FluentAssertions;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.CustomerSupport.Api.Domain;
using FreshCart.CustomerSupport.Api.Persistence;
using FreshCart.CustomerSupport.Tests.Support;
using MongoDB.Driver;
using Xunit;

namespace FreshCart.CustomerSupport.Tests.Persistence;

[Collection(MongoFixture.CollectionName)]
public sealed class MongoChatRepositoryTests : IDisposable
{
    private static readonly Guid CustomerId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherCustomerId = Guid.Parse("c0000000-0000-0000-0000-000000000002");
    private static readonly Guid AgentId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Start = new(2026, 6, 18, 8, 0, 0, TimeSpan.Zero);

    private const string CustomerName = "Dana Customer";
    private const string AgentName = "Ravi Agent";

    private readonly MongoClient _mongoClient;
    private readonly MongoChatSessionRepository _sessionRepository;
    private readonly MongoChatMessageRepository _messageRepository;

    public MongoChatRepositoryTests(MongoFixture mongoFixture)
    {
        ArgumentNullException.ThrowIfNull(mongoFixture);

        _mongoClient = new MongoClient(mongoFixture.ConnectionString);

        var isolatedDatabaseName = $"supportchat_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";
        var context = new SupportChatMongoContext(_mongoClient.GetDatabase(isolatedDatabaseName));
        _sessionRepository = new MongoChatSessionRepository(context);
        _messageRepository = new MongoChatMessageRepository(context);
    }

    public void Dispose() => _mongoClient.Dispose();

    [Fact]
    public async Task SavingASessionTwiceUpsertsRatherThanDuplicating()
    {
        var session = ChatSession.Start(Guid.CreateVersion7(), "Order enquiry", CustomerId, CustomerName, Start);
        await _sessionRepository.SaveAsync(session, CancellationToken.None);

        session.AssignTo(AgentId, AgentName);
        await _sessionRepository.SaveAsync(session, CancellationToken.None);

        var reloaded = await _sessionRepository.GetByIdAsync(session.Id, CancellationToken.None);
        reloaded!.Status.Should().Be(SessionStatus.Active);
        reloaded.AgentId.Should().Be(AgentId);

        var allForCustomer = await _sessionRepository.GetSessionsAsync(
            statusFilter: null, new PaginationRequest(1, 50), CancellationToken.None);
        allForCustomer.TotalItemCount.Should().Be(1, "the second save updated the same document");
    }

    [Fact]
    public async Task OnlyTheCustomersOwnOpenSessionIsReturned()
    {
        var openSession = ChatSession.Start(Guid.CreateVersion7(), "Open", CustomerId, CustomerName, Start);
        var endedSession = ChatSession.Start(Guid.CreateVersion7(), "Ended", CustomerId, CustomerName, Start.AddMinutes(-30));
        endedSession.End(Start.AddMinutes(-20));
        var otherPersonsSession = ChatSession.Start(Guid.CreateVersion7(), "Other", OtherCustomerId, "Someone", Start);

        await _sessionRepository.SaveAsync(openSession, CancellationToken.None);
        await _sessionRepository.SaveAsync(endedSession, CancellationToken.None);
        await _sessionRepository.SaveAsync(otherPersonsSession, CancellationToken.None);

        var result = await _sessionRepository.GetOpenSessionForCustomerAsync(CustomerId, CancellationToken.None);

        result!.Id.Should().Be(openSession.Id);
    }

    [Fact]
    public async Task OnlyTheAgentsActiveSessionsAreReturned()
    {
        var activeSession = ChatSession.Start(Guid.CreateVersion7(), "Active", CustomerId, CustomerName, Start);
        activeSession.AssignTo(AgentId, AgentName);
        var endedSession = ChatSession.Start(Guid.CreateVersion7(), "Ended", OtherCustomerId, "Someone", Start);
        endedSession.AssignTo(AgentId, AgentName);
        endedSession.End(Start.AddMinutes(5));

        await _sessionRepository.SaveAsync(activeSession, CancellationToken.None);
        await _sessionRepository.SaveAsync(endedSession, CancellationToken.None);

        var agentSessions = await _sessionRepository.GetActiveSessionsForAgentAsync(AgentId, CancellationToken.None);

        agentSessions.Should().ContainSingle().Which.Id.Should().Be(activeSession.Id);
    }

    [Fact]
    public async Task SessionListCanBeFilteredByStatus()
    {
        var queued = ChatSession.Start(Guid.CreateVersion7(), "Queued", CustomerId, CustomerName, Start);
        var ended = ChatSession.Start(Guid.CreateVersion7(), "Ended", OtherCustomerId, "Someone", Start.AddMinutes(1));
        ended.End(Start.AddMinutes(2));

        await _sessionRepository.SaveAsync(queued, CancellationToken.None);
        await _sessionRepository.SaveAsync(ended, CancellationToken.None);

        var endedOnly = await _sessionRepository.GetSessionsAsync(
            SessionStatus.Ended, new PaginationRequest(1, 20), CancellationToken.None);

        endedOnly.TotalItemCount.Should().Be(1);
        endedOnly.Items.Should().ContainSingle().Which.Id.Should().Be(ended.Id);
    }

    [Fact]
    public async Task TranscriptPagingReturnsMessagesAscendingBySendTimeAcrossPages()
    {
        var sessionId = Guid.CreateVersion7();
        for (var index = 0; index < 5; index++)
        {
            var message = ChatMessage.Create(
                Guid.CreateVersion7(),
                sessionId,
                CustomerId,
                CustomerName,
                SenderRole.Customer,
                string.Create(CultureInfo.InvariantCulture, $"Message {index}"),
                Start.AddMinutes(index));

            await _messageRepository.AddAsync(message, CancellationToken.None);
        }

        var firstPage = await _messageRepository.GetSessionMessagesAsync(
            sessionId, new PaginationRequest(1, 2), CancellationToken.None);
        var secondPage = await _messageRepository.GetSessionMessagesAsync(
            sessionId, new PaginationRequest(2, 2), CancellationToken.None);

        firstPage.TotalItemCount.Should().Be(5);
        firstPage.Items.Select(message => message.Text).Should().Equal("Message 0", "Message 1");
        secondPage.Items.Select(message => message.Text).Should().Equal("Message 2", "Message 3");
    }

    [Fact]
    public async Task TranscriptPagingIsScopedToTheRequestedSession()
    {
        var sessionId = Guid.CreateVersion7();
        var otherSessionId = Guid.CreateVersion7();

        await _messageRepository.AddAsync(
            ChatMessage.Create(Guid.CreateVersion7(), sessionId, CustomerId, CustomerName, SenderRole.Customer, "Mine", Start),
            CancellationToken.None);
        await _messageRepository.AddAsync(
            ChatMessage.Create(Guid.CreateVersion7(), otherSessionId, OtherCustomerId, "Someone", SenderRole.Customer, "Theirs", Start),
            CancellationToken.None);

        var page = await _messageRepository.GetSessionMessagesAsync(
            sessionId, new PaginationRequest(1, 20), CancellationToken.None);

        page.TotalItemCount.Should().Be(1);
        page.Items.Should().ContainSingle().Which.Text.Should().Be("Mine");
    }
}
