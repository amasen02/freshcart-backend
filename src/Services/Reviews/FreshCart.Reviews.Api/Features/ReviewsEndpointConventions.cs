namespace FreshCart.Reviews.Api.Features;

public static class ReviewsEndpointConventions
{
    public const string ReviewsRoute = "/reviews";
    public const string ProductReviewsRoute = "/reviews/product/{productSku}";
    public const string MyReviewsRoute = "/reviews/mine";
    public const string PendingReviewsRoute = "/reviews/pending";
    public const string ModerationRoute = "/reviews/{reviewId:guid}/moderation";

    public const string ReviewsTag = "Reviews";
}
