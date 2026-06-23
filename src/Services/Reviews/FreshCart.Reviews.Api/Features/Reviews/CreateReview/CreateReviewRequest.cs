namespace FreshCart.Reviews.Api.Features.Reviews.CreateReview;

/// <summary>
/// The review fields a customer supplies. The author identity is taken from the access token in the
/// endpoint, never from the body, so it is deliberately absent here.
/// </summary>
public sealed record CreateReviewRequest(string ProductSku, int Rating, string Title, string Body);
