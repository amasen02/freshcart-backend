using Carter;
using FluentValidation;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Pricing.Grpc.Configuration;
using FreshCart.Pricing.Grpc.Models;
using FreshCart.Pricing.Grpc.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Pricing.Grpc.Endpoints.Coupons;

/// <summary>
/// Carter module for admin-managed coupon codes. Validation runs explicitly in the endpoint
/// because this service has no MediatR pipeline; the shared exception handler maps
/// <see cref="ValidationException"/> to a 400 ProblemDetails response.
/// </summary>
public sealed class CouponEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var couponGroup = app
            .MapGroup("/pricing")
            .RequireAuthorization(AuthorizationPolicyNames.Administrator)
            .WithTags("Coupons");

        couponGroup.MapPost("/coupons", CreateCouponAsync)
            .WithSummary("Create a coupon code.")
            .Produces<CouponResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        couponGroup.MapGet("/coupons", GetCouponsAsync)
            .WithSummary("List coupon codes, paginated.")
            .Produces<PaginatedResult<CouponResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateCouponAsync(
        CreateCouponRequest request,
        IValidator<CreateCouponRequest> validator,
        PricingDbContext pricingDbContext,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);

        var couponCodeExists = await pricingDbContext.CouponCodes
            .AsNoTracking()
            .AnyAsync(coupon => coupon.Code == request.Code, cancellationToken)
            .ConfigureAwait(false);

        if (couponCodeExists)
        {
            throw new ConflictException($"Coupon code '{request.Code}' already exists.");
        }

        var coupon = new CouponCode
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinimumOrderAmount = request.MinimumOrderAmount,
            UsageLimit = request.UsageLimit,
            UsageCount = 0,
            ValidFromUtc = request.ValidFromUtc,
            ValidToUtc = request.ValidToUtc,
            IsActive = request.IsActive,
        };

        pricingDbContext.CouponCodes.Add(coupon);
        await pricingDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Results.Created($"/pricing/coupons/{coupon.Id}", ToResponse(coupon));
    }

    private static async Task<IResult> GetCouponsAsync(
        [AsParameters] PaginationRequest paginationRequest,
        PricingDbContext pricingDbContext,
        CancellationToken cancellationToken)
    {
        var pagination = paginationRequest.Normalise();

        var totalCouponCount = await pricingDbContext.CouponCodes
            .LongCountAsync(cancellationToken)
            .ConfigureAwait(false);

        var coupons = await pricingDbContext.CouponCodes
            .AsNoTracking()
            .OrderBy(coupon => coupon.Code)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(coupon => new CouponResponse(
                coupon.Id,
                coupon.Code,
                coupon.DiscountType,
                coupon.DiscountValue,
                coupon.MinimumOrderAmount,
                coupon.UsageLimit,
                coupon.UsageCount,
                coupon.ValidFromUtc,
                coupon.ValidToUtc,
                coupon.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(new PaginatedResult<CouponResponse>(
            pagination.PageNumber,
            pagination.PageSize,
            totalCouponCount,
            coupons));
    }

    private static CouponResponse ToResponse(CouponCode coupon) =>
        new(
            coupon.Id,
            coupon.Code,
            coupon.DiscountType,
            coupon.DiscountValue,
            coupon.MinimumOrderAmount,
            coupon.UsageLimit,
            coupon.UsageCount,
            coupon.ValidFromUtc,
            coupon.ValidToUtc,
            coupon.IsActive);
}
