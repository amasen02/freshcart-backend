using MongoDB.Driver;

namespace FreshCart.CustomerSupport.Api.Persistence;

/// <summary>
/// Single place that knows the support database name and its two collection names, so the
/// repositories and the index initializer all resolve the same collections.
/// </summary>
public sealed class SupportChatMongoContext
{
    public const string ConnectionStringName = "supportchatdb";
    public const string DefaultDatabaseName = "supportchatdb";
    public const string SessionsCollectionName = "chat_sessions";
    public const string MessagesCollectionName = "chat_messages";

    static SupportChatMongoContext()
    {
        // Driver 3.x maps a nullable Guid through NullableSerializer, which rejects a per-property
        // [BsonGuidRepresentation] attribute, so the representation is set once globally here before
        // any document type is mapped. This must run before the first GetCollection call.
        SupportChatGuidSerialization.EnsureRegistered();
    }

    public SupportChatMongoContext(IMongoDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        Sessions = database.GetCollection<ChatSessionDocument>(SessionsCollectionName);
        Messages = database.GetCollection<ChatMessageDocument>(MessagesCollectionName);
    }

    public IMongoCollection<ChatSessionDocument> Sessions { get; }

    public IMongoCollection<ChatMessageDocument> Messages { get; }
}
