namespace FreshCart.Notification.Api.Channels;

/// <summary>
/// Transport port for outbound mail. The Development implementation logs the rendered message;
/// SMTP and SendGrid adapters implement this same interface in production.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
