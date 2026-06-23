namespace FreshCart.BuildingBlocks.Security;

/// <summary>
/// Bound from the <c>OutboundUrlAllowList</c> configuration section.
/// </summary>
public sealed class OutboundUrlAllowListOptions
{
    public const string SectionName = "OutboundUrlAllowList";

    public IReadOnlyCollection<string> AllowedHosts { get; init; } = Array.Empty<string>();
}
