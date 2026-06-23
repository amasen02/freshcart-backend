using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.CustomerSupport.Api.Domain;
using Xunit;

namespace FreshCart.CustomerSupport.Tests.Domain;

public sealed class ChatSessionTests
{
    private static readonly Guid CustomerId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid AgentId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Start = new(2026, 6, 18, 9, 0, 0, TimeSpan.Zero);

    private const string Topic = "Order enquiry";
    private const string CustomerName = "Dana Customer";
    private const string AgentName = "Ravi Agent";

    [Fact]
    public void AStartedSessionBeginsQueuedWithNoAgent()
    {
        var session = ChatSession.Start(Guid.CreateVersion7(), Topic, CustomerId, CustomerName, Start);

        session.Status.Should().Be(SessionStatus.Queued);
        session.AgentId.Should().BeNull();
        session.EndedOnUtc.Should().BeNull();
    }

    [Fact]
    public void AssigningASessionMakesItActiveAndRecordsTheAgent()
    {
        var session = ChatSession.Start(Guid.CreateVersion7(), Topic, CustomerId, CustomerName, Start);

        session.AssignTo(AgentId, AgentName);

        session.Status.Should().Be(SessionStatus.Active);
        session.AgentId.Should().Be(AgentId);
        session.AgentDisplayName.Should().Be(AgentName);
    }

    [Fact]
    public void ReturningASessionToTheQueueClearsTheAgent()
    {
        var session = ChatSession.Start(Guid.CreateVersion7(), Topic, CustomerId, CustomerName, Start);
        session.AssignTo(AgentId, AgentName);

        session.ReturnToQueue();

        session.Status.Should().Be(SessionStatus.Queued);
        session.AgentId.Should().BeNull();
        session.AgentDisplayName.Should().BeNull();
    }

    [Fact]
    public void EndingASessionRecordsTheEndInstant()
    {
        var session = ChatSession.Start(Guid.CreateVersion7(), Topic, CustomerId, CustomerName, Start);
        var endInstant = Start.AddMinutes(12);

        session.End(endInstant);

        session.Status.Should().Be(SessionStatus.Ended);
        session.EndedOnUtc.Should().Be(endInstant);
    }

    [Fact]
    public void AnEndedSessionCannotBeAssigned()
    {
        var session = ChatSession.Start(Guid.CreateVersion7(), Topic, CustomerId, CustomerName, Start);
        session.End(Start.AddMinutes(1));

        var assignEnded = () => session.AssignTo(AgentId, AgentName);

        assignEnded.Should().Throw<BadRequestException>();
    }

    [Fact]
    public void EndingAnAlreadyEndedSessionIsRejected()
    {
        var session = ChatSession.Start(Guid.CreateVersion7(), Topic, CustomerId, CustomerName, Start);
        session.End(Start.AddMinutes(1));

        var endAgain = () => session.End(Start.AddMinutes(2));

        endAgain.Should().Throw<BadRequestException>();
    }

    [Fact]
    public void TheCustomerAndTheAssignedAgentAreBothParticipants()
    {
        var session = ChatSession.Start(Guid.CreateVersion7(), Topic, CustomerId, CustomerName, Start);
        session.AssignTo(AgentId, AgentName);

        session.IsParticipant(CustomerId).Should().BeTrue();
        session.IsParticipant(AgentId).Should().BeTrue();
        session.IsParticipant(Guid.NewGuid()).Should().BeFalse();
    }
}
