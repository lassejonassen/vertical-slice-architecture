namespace VerticalSliceArchitecture.SharedKernel.Results;

/// <summary>
/// Composition helpers. These keep handlers free of nested <c>if (result.IsFailure)</c> ladders.
/// HTTP-specific mapping lives in the API project, not here — the SharedKernel stays transport agnostic.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Projects the value of a successful result. Failures pass through untouched.</summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map) =>
        result.IsSuccess ? Result.Success(map(result.Value)) : Result.Failure<TOut>(result.Error);

    /// <summary>Chains an operation that itself returns a result.</summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> bind) =>
        result.IsSuccess ? bind(result.Value) : Result.Failure<TOut>(result.Error);

    public static Result Bind<TIn>(this Result<TIn> result, Func<TIn, Result> bind) =>
        result.IsSuccess ? bind(result.Value) : Result.Failure(result.Error);

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> bind) =>
        result.IsSuccess ? await bind(result.Value) : Result.Failure<TOut>(result.Error);

    /// <summary>Runs a side effect on success without changing the result.</summary>
    public static Result<TValue> Tap<TValue>(this Result<TValue> result, Action<TValue> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>Collapses both branches into a single value.</summary>
    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Error, TOut> onFailure) =>
        result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);

    public static TOut Match<TOut>(
        this Result result,
        Func<TOut> onSuccess,
        Func<Error, TOut> onFailure) =>
        result.IsSuccess ? onSuccess() : onFailure(result.Error);

    /// <summary>Converts a nullable reference into a result, using <paramref name="error"/> when null.</summary>
    public static Result<TValue> ToResult<TValue>(this TValue? value, Error error)
        where TValue : class =>
        value is null ? Result.Failure<TValue>(error) : Result.Success(value);
}
