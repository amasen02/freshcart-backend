namespace FreshCart.Reporting.Infrastructure;

/// <summary>
/// Bound from the <c>BlobStorage</c> configuration section. Exactly one of
/// <see cref="ConnectionString"/> (local emulator) or <see cref="ServiceUri"/> (managed identity)
/// must be supplied.
/// </summary>
public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string? ConnectionString { get; init; }

    public string? ServiceUri { get; init; }
}
