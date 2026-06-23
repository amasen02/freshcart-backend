using MassTransit;
using NSubstitute;

namespace FreshCart.Notification.Tests.Support;

/// <summary>Builds a substitute <see cref="ConsumeContext{T}"/> carrying a message and token.</summary>
public static class ConsumeContextFactory
{
    public static ConsumeContext<TMessage> For<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class
    {
        var consumeContext = Substitute.For<ConsumeContext<TMessage>>();
        consumeContext.Message.Returns(message);
        consumeContext.CancellationToken.Returns(cancellationToken);
        return consumeContext;
    }
}
