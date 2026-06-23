using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using FreshCart.BuildingBlocks.Behaviors;
using FreshCart.BuildingBlocks.CQRS;
using MediatR;
using NSubstitute;

namespace FreshCart.BuildingBlocks.Tests.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task HandleInvokesNextWhenNoValidatorsAreRegistered()
    {
        var behavior = new ValidationBehavior<DummyCommand, string>(Array.Empty<IValidator<DummyCommand>>());
        var nextWasInvoked = false;

        var response = await behavior.Handle(
            new DummyCommand(),
            () => { nextWasInvoked = true; return Task.FromResult("ok"); },
            CancellationToken.None);

        response.Should().Be("ok");
        nextWasInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task HandleInvokesNextWhenEveryValidatorPasses()
    {
        var passingValidator = Substitute.For<IValidator<DummyCommand>>();
        passingValidator
            .ValidateAsync(Arg.Any<ValidationContext<DummyCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<DummyCommand, string>(new[] { passingValidator });

        var response = await behavior.Handle(
            new DummyCommand(),
            () => Task.FromResult("ok"),
            CancellationToken.None);

        response.Should().Be("ok");
    }

    [Fact]
    public Task HandleThrowsValidationExceptionWhenAnyValidatorFails()
    {
        var failingValidator = Substitute.For<IValidator<DummyCommand>>();
        failingValidator
            .ValidateAsync(Arg.Any<ValidationContext<DummyCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Email", "Email is required.") }));

        var behavior = new ValidationBehavior<DummyCommand, string>(new[] { failingValidator });

        Func<Task> act = () => behavior.Handle(
            new DummyCommand(),
            () => Task.FromResult("ok"),
            CancellationToken.None);

        return act.Should().ThrowAsync<ValidationException>()
            .Where(exception => exception.Errors.Any(failure => failure.PropertyName == "Email"));
    }

    [Fact]
    public Task HandleThrowsArgumentNullExceptionWhenRequestIsNull()
    {
        var behavior = new ValidationBehavior<DummyCommand, string>(Array.Empty<IValidator<DummyCommand>>());

        Func<Task> act = () => behavior.Handle(null!, () => Task.FromResult("ok"), CancellationToken.None);

        return act.Should().ThrowAsync<ArgumentNullException>();
    }

    public sealed record DummyCommand : ICommand<string>;
}
