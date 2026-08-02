namespace VerticalSliceArchitecture.SharedKernel.Results;

/// <summary>
/// Outcome of an operation that can fail in an expected way.
/// <para>
/// Rule of thumb used throughout this template: return <see cref="Result"/> for anything a
/// caller could reasonably anticipate and handle (validation, missing rows, illegal state
/// transitions). Throw only for programmer error and infrastructure faults — those are
/// caught by <c>GlobalExceptionHandler</c> and reported as 500.
/// </para>
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        switch (isSuccess)
        {
            case true when error != Error.None:
                throw new InvalidOperationException("A successful result cannot carry an error.");
            case false when error == Error.None:
                throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>
    /// Returns the first failure among <paramref name="results"/>, or success if there is none.
    /// Useful for validating several value objects before constructing an aggregate.
    /// </summary>
    public static Result FirstFailureOrSuccess(params Result[] results)
    {
        foreach (Result result in results)
        {
            if (result.IsFailure)
            {
                return Failure(result.Error);
            }
        }

        return Success();
    }

    /// <summary>Like <see cref="FirstFailureOrSuccess"/> but reports every failure at once.</summary>
    public static Result AllOrValidationError(params Result[] results)
    {
        Error[] errors = [.. results.Where(r => r.IsFailure).Select(r => r.Error)];

        return errors.Length == 0 ? Success() : Failure(new ValidationError(errors));
    }

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>A <see cref="Result"/> that carries a value when successful.</summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    /// <summary>The value. Throws when the result is a failure — check <see cref="Result.IsSuccess"/> first.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
