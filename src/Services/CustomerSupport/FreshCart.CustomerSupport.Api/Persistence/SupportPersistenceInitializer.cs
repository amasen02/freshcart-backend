using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FreshCart.CustomerSupport.Api.Persistence;

/// <summary>
/// Creates the transcript and session indexes on startup so history paging and the open-session and
/// agent-active lookups are index-backed from the first request rather than degrading to collection
/// scans as the data grows.
/// </summary>
public sealed partial class SupportPersistenceInitializer(
    SupportChatMongoContext mongoContext,
    ILogger<SupportPersistenceInitializer> logger) : IHostedService
{
    private const string MessagesSessionTimelineIndexName = "IX_chat_messages_SessionId_SentOnUtc";
    private const string SessionsCustomerTimelineIndexName = "IX_chat_sessions_CustomerId_StartedOnUtc";
    private const string SessionsAgentStatusIndexName = "IX_chat_sessions_AgentId_Status";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var messageTimelineIndex = new CreateIndexModel<ChatMessageDocument>(
            Builders<ChatMessageDocument>.IndexKeys
                .Ascending(document => document.SessionId)
                .Ascending(document => document.SentOnUtc),
            new CreateIndexOptions { Name = MessagesSessionTimelineIndexName });

        var customerTimelineIndex = new CreateIndexModel<ChatSessionDocument>(
            Builders<ChatSessionDocument>.IndexKeys
                .Ascending(document => document.CustomerId)
                .Descending(document => document.StartedOnUtc),
            new CreateIndexOptions { Name = SessionsCustomerTimelineIndexName });

        var agentStatusIndex = new CreateIndexModel<ChatSessionDocument>(
            Builders<ChatSessionDocument>.IndexKeys
                .Ascending(document => document.AgentId)
                .Ascending(document => document.Status),
            new CreateIndexOptions { Name = SessionsAgentStatusIndexName });

        await mongoContext.Messages.Indexes
            .CreateOneAsync(messageTimelineIndex, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await mongoContext.Sessions.Indexes
            .CreateManyAsync([customerTimelineIndex, agentStatusIndex], cancellationToken)
            .ConfigureAwait(false);

        LogPersistenceVerified();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Support chat persistence verified")]
    private partial void LogPersistenceVerified();
}
