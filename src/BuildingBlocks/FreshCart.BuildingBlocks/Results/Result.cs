namespace FreshCart.BuildingBlocks.Results;

/// <summary>
/// Lightweight result envelope used where exceptions would be coarse, for example when validating a
/// pure-domain method that does not justify a thrown <see cref="Exceptions.DomainException"/>.
/// Application layer code should prefer exceptions; this type exists for the pure domain and for
/// integration points where re-throwing across process boundaries would be wasteful.
/// The generic factories live here so <see cref="Result{TValue}"/> carries no static members.
/// </summary>
public readonly record struct Result(bool IsSuccess, string? Error)
{
    public static Result Success() => new(true, null);

    public static Result<TValue> Success<TValue>(TValue value) => new(true, value, null);

    public static Result Failure(string error) => new(false, error);

    public static Result<TValue> Failure<TValue>(string error) => new(false, default, error);
}

/// <summary>
/// Value-carrying counterpart of <see cref="Result"/>; construct it via the factories on
/// <see cref="Result"/>.
/// </summary>
public readonly record struct Result<TValue>(bool IsSuccess, TValue? Value, string? Error);
