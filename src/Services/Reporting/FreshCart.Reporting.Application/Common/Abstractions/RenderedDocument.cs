namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Finished downloadable document produced by a renderer or exporter.
/// </summary>
public sealed record RenderedDocument(
    string FileName,
    string ContentType,
    byte[] Content);
