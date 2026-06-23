using FreshCart.BuildingBlocks.Messaging.Outbox;
using FluentAssertions;

namespace FreshCart.BuildingBlocks.Tests.Messaging;

public sealed class OutboxMessageTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 23, 12, 0, 0, TimeSpan.Zero);

    private static OutboxMessage NewMessage() => new()
    {
        EventType = "FreshCart.Some.IntegrationEvent",
        ContentJson = "{}",
    };

    [Fact]
    public void AFailureBelowTheRetryCeilingBumpsTheAttemptWithoutDeadLettering()
    {
        var message = NewMessage();

        var deadLettered = message.MarkFailed("broker timeout", maxRetryAttempts: 5, NowUtc);

        deadLettered.Should().BeFalse();
        message.RetryAttempt.Should().Be(1);
        message.ProcessedOnUtc.Should().BeNull("the message must stay pollable for the next retry");
        message.IsDeadLettered.Should().BeFalse();
        message.Error.Should().Be("broker timeout");
    }

    [Fact]
    public void ReachingTheRetryCeilingDeadLettersTheMessageSoItIsNoLongerPolled()
    {
        var message = NewMessage();
        const int maxRetryAttempts = 3;

        message.MarkFailed("fail 1", maxRetryAttempts, NowUtc);
        message.MarkFailed("fail 2", maxRetryAttempts, NowUtc);
        var deadLettered = message.MarkFailed("fail 3", maxRetryAttempts, NowUtc);

        deadLettered.Should().BeTrue();
        message.RetryAttempt.Should().Be(maxRetryAttempts);
        message.ProcessedOnUtc.Should().Be(NowUtc, "a dead-lettered message is stamped processed so GetUnpublished stops returning it");
        message.IsDeadLettered.Should().BeTrue();
        message.Error.Should().StartWith(OutboxMessage.DeadLetterErrorPrefix);
        message.Error.Should().Contain("fail 3");
    }

    [Fact]
    public void ASuccessfullyPublishedMessageIsNotConsideredDeadLettered()
    {
        var message = NewMessage();

        message.ProcessedOnUtc = NowUtc;
        message.Error = null;

        message.IsDeadLettered.Should().BeFalse("a published message has a processed stamp but no recorded error");
    }

    [Fact]
    public void TheStoredFailureReasonIsTruncatedToTheColumnLength()
    {
        var message = NewMessage();
        var hugeError = new string('x', (OutboxMessage.MaxStoredErrorLength * 2) + 7);

        message.MarkFailed(hugeError, maxRetryAttempts: 5, NowUtc);

        message.Error!.Length.Should().Be(OutboxMessage.MaxStoredErrorLength);
    }

    [Fact]
    public void MarkFailedRejectsANonPositiveRetryCeiling()
    {
        var message = NewMessage();

        var act = () => message.MarkFailed("boom", maxRetryAttempts: 0, NowUtc);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
