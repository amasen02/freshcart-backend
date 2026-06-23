namespace FreshCart.Pricing.Grpc.Endpoints.DiscountRules;

public sealed record CreateDiscountRuleRequest(
    Guid ProductId,
    string Name,
    decimal DiscountPercentage,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ValidToUtc,
    bool IsActive);
