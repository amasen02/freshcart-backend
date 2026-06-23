namespace FreshCart.Pricing.Grpc.Endpoints.DiscountRules;

public sealed record DiscountRuleResponse(
    Guid Id,
    Guid ProductId,
    string Name,
    decimal DiscountPercentage,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ValidToUtc,
    bool IsActive);
