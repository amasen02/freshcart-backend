namespace FreshCart.CustomerSupport.Api.Domain;

/// <summary>
/// One line of a transcript. Messages are immutable once authored, so the type carries no mutators:
/// editing history would defeat the purpose of keeping an auditable conversation record.
/// </summary>
public sealed class ChatMessage
{
    private ChatMessage(
        Guid id,
        Guid sessionId,
        Guid senderId,
        string senderDisplayName,
        SenderRole senderRole,
        string text,
        DateTimeOffset sentOnUtc)
    {
        Id = id;
        SessionId = sessionId;
        SenderId = senderId;
        SenderDisplayName = senderDisplayName;
        SenderRole = senderRole;
        Text = text;
        SentOnUtc = sentOnUtc;
    }

    public Guid Id { get; }

    public Guid SessionId { get; }

    public Guid SenderId { get; }

    public string SenderDisplayName { get; }

    public SenderRole SenderRole { get; }

    public string Text { get; }

    public DateTimeOffset SentOnUtc { get; }

    public static ChatMessage Create(
        Guid id,
        Guid sessionId,
        Guid senderId,
        string senderDisplayName,
        SenderRole senderRole,
        string text,
        DateTimeOffset sentOnUtc) =>
        new(id, sessionId, senderId, senderDisplayName, senderRole, text, sentOnUtc);
}
