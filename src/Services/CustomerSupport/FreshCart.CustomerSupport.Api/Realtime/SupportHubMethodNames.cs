namespace FreshCart.CustomerSupport.Api.Realtime;

/// <summary>
/// The server-to-client method names the SignalR client subscribes to. Naming them once stops a typo
/// in one call site from silently dropping a notification that the Angular client is waiting on.
/// </summary>
public static class SupportHubMethodNames
{
    public const string ChatAssigned = "chatAssigned";

    public const string MessageReceived = "messageReceived";

    public const string ParticipantTyping = "participantTyping";

    public const string ChatEnded = "chatEnded";

    public const string QueuePositionChanged = "queuePositionChanged";
}
