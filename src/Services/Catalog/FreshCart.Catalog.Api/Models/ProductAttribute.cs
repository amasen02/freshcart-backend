using System.Diagnostics.CodeAnalysis;

namespace FreshCart.Catalog.Api.Models;

[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Domain term for a name/value pair on the product document; unrelated to System.Attribute.")]
public sealed record ProductAttribute(string Name, string Value);
