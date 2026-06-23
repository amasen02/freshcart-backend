namespace FreshCart.Reviews.Api.Domain;

/// <summary>
/// The business limits that define a well-formed review. They live in one place so the validator, the
/// document model and the tests all encode the same rule rather than scattering magic numbers.
/// </summary>
public static class ReviewConstraints
{
    public const int MinimumRating = 1;
    public const int MaximumRating = 5;

    public const int MinTitleLength = 5;
    public const int MaxTitleLength = 120;

    public const int MinBodyLength = 10;
    public const int MaxBodyLength = 4000;

    public const int MaxProductSkuLength = 64;
}
