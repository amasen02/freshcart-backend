namespace FreshCart.Reviews.Api.Domain;

/// <summary>
/// Lifecycle of a <see cref="ProductReview"/>. New reviews start <see cref="Pending"/> so a moderator
/// gates them before they reach the storefront; only <see cref="Approved"/> reviews are publicly visible.
/// </summary>
public enum ReviewStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
