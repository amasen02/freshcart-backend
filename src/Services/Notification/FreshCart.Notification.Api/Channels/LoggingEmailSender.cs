using Microsoft.Extensions.Logging;

namespace FreshCart.Notification.Api.Channels;

/// <summary>
/// Development <see cref="IEmailSender"/> that records the rendered mail in the structured log rather
/// than dispatching it. SMTP and SendGrid adapters replace it in higher environments.
/// </summary>
public sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        LogRenderedMail(message.Subject, message.PlainTextBody);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Email channel rendered mail with subject \"{Subject}\": {Body}")]
    private partial void LogRenderedMail(string subject, string body);
}
