using Carter;
using FluentValidation;
using FreshCart.Pricing.Grpc.Configuration;
using FreshCart.Pricing.Grpc.Models;
using FreshCart.Pricing.Grpc.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Pricing.Grpc.Endpoints.DiscountRules;

/// <summary>
/// Carter module for admin-managed discount rules. Validation runs explicitly in the endpoint
/// because this service has no MediatR pipeline; the shared exception handler maps
/// <see cref="ValidationException"/> to a 400 ProblemDetails response.
/// </summary>
public sealed class DiscountRuleEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var discountRuleGroup = app
            .MapGroup("/pricing")
            .RequireAuthorization(AuthorizationPolicyNames.Administrator)
            .WithTags("Discount rules");

        discountRuleGroup.MapPost("/discount-rules", CreateDiscountRuleAsync)
            .WithSummary("Create a product discount rule.")
            .Produces<DiscountRuleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        discountRuleGroup.MapGet("/discount-rules", GetDiscountRulesAsync)
            .WithSummary("List discount rules, optionally filtered by product.")
            .Produces<IReadOnlyList<DiscountRuleResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateDiscountRuleAsync(
        CreateDiscountRuleRequest request,
        IValidator<CreateDiscountRuleRequest> validator,
        PricingDbContext pricingDbContext,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);

        var discountRule = new DiscountRule
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Name = request.Name,
            DiscountPercentage = request.DiscountPercentage,
            ValidFromUtc = request.ValidFromUtc,
            ValidToUtc = request.ValidToUtc,
            IsActive = request.IsActive,
        };

        pricingDbContext.DiscountRules.Add(discountRule);
        await pricingDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Results.Created($"/pricing/discount-rules/{discountRule.Id}", ToResponse(discountRule));
    }

    private static async Task<IResult> GetDiscountRulesAsync(
        Guid? productId,
        PricingDbContext pricingDbContext,
        CancellationToken cancellationToken)
    {
        var discountRulesQuery = pricingDbContext.DiscountRules.AsNoTracking();

        if (productId.HasValue)
        {
            discountRulesQuery = discountRulesQuery.Where(discountRule => discountRule.ProductId == productId.Value);
        }

        var discountRules = await discountRulesQuery
            .OrderBy(discountRule => discountRule.Name)
            .Select(discountRule => new DiscountRuleResponse(
                discountRule.Id,
                discountRule.ProductId,
                discountRule.Name,
                discountRule.DiscountPercentage,
                discountRule.ValidFromUtc,
                discountRule.ValidToUtc,
                discountRule.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(discountRules);
    }

    private static DiscountRuleResponse ToResponse(DiscountRule discountRule) =>
        new(
            discountRule.Id,
            discountRule.ProductId,
            discountRule.Name,
            discountRule.DiscountPercentage,
            discountRule.ValidFromUtc,
            discountRule.ValidToUtc,
            discountRule.IsActive);
}
