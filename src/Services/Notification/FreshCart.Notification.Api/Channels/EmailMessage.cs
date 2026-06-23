namespace FreshCart.Notification.Api.Channels;

/// <summary>
/// A rendered, transport-agnostic mail. The body is plain text so any sender adapter can deliver it
/// without re-rendering.
/// </summary>
public sealed record EmailMessage(string Subject, string PlainTextBody);
