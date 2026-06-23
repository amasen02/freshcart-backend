using FluentAssertions;
using FreshCart.BuildingBlocks.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FreshCart.BuildingBlocks.Tests.Behaviors;

public sealed class LoggingBehaviorTests
{
    [Fact]
    public async Task HandleReturnsTheResponseProducedByTheNextDelegate()
    {
        var behavior = new LoggingBehavior<DummyRequest, string>(NullLogger());

        var response = await behavior.Handle(
            new DummyRequest("payload"),
            () => Task.FromResult("ok"),
            CancellationToken.None);

        response.Should().Be("ok");
    }

    [Fact]
    public async Task HandleRethrowsExceptionsAndLogsAnError()
    {
        var logger = Substitute.For<ILogger<LoggingBehavior<DummyRequest, string>>>();
        logger.IsEnabled(LogLevel.Error).Returns(true);
        var behavior = new LoggingBehavior<DummyRequest, string>(logger);

        Func<Task> act = () => behavior.Handle(
            new DummyRequest("payload"),
            () => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

        var errorLogCall = logger.ReceivedCalls()
            .Single(call => string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal)
                && (LogLevel)call.GetArguments()[0]! == LogLevel.Error);
        errorLogCall.GetArguments()[3].Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public Task HandleThrowsArgumentNullExceptionWhenRequestIsNull()
    {
        var behavior = new LoggingBehavior<DummyRequest, string>(NullLogger());

        Func<Task> act = () => behavior.Handle(null!, () => Task.FromResult("ok"), CancellationToken.None);

        return act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static ILogger<LoggingBehavior<DummyRequest, string>> NullLogger() =>
        Substitute.For<ILogger<LoggingBehavior<DummyRequest, string>>>();

    public sealed record DummyRequest(string Payload) : IRequest<string>;
}
