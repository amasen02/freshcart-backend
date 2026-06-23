namespace FreshCart.Reviews.Api.Domain;

/// <summary>
/// Outcome a moderator may record against a pending review. A decision maps to the terminal
/// <see cref="ReviewStatus"/> the review transitions into; there is deliberately no decision that
/// returns a review to <see cref="ReviewStatus.Pending"/>.
/// </summary>
public enum ModerationDecision
{
    Approved = 1,
    Rejected = 2,
}
