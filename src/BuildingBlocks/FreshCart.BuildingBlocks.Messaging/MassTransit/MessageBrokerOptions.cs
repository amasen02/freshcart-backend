namespace FreshCart.BuildingBlocks.Messaging.MassTransit;

/// <summary>
/// Bound from the <c>MessageBroker</c> configuration section.
/// </summary>
public sealed class MessageBrokerOptions
{
    public const string SectionName = "MessageBroker";

    public required string Host { get; init; }

    public string? UserName { get; init; }

    public string? Password { get; init; }
}
