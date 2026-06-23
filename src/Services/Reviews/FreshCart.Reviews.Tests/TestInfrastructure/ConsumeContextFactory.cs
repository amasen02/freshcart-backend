using MassTransit;
using NSubstitute;

namespace FreshCart.Reviews.Tests.TestInfrastructure;

/// <summary>Builds a substitute <see cref="ConsumeContext{T}"/> carrying a message and token.</summary>
internal static class ConsumeContextFactory
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
