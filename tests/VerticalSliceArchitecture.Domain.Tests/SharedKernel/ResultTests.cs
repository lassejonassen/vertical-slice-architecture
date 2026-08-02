using Shouldly;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Tests.SharedKernel;

public class ResultTests
{
    private static readonly Error SampleError = Error.Failure("Sample.Error", "Something went wrong.");

    [Fact]
    public void Success_HasNoError()
    {
        Result result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_CarriesTheGivenError()
    {
        Result result = Result.Failure(SampleError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void GenericSuccess_ExposesTheValue()
    {
        Result<int> result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void GenericFailure_ThrowsWhenTheValueIsAccessed()
    {
        Result<int> result = Result.Failure<int>(SampleError);

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromErrorToResult_ProducesAFailure()
    {
        Result result = SampleError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void ImplicitConversion_FromValueToGenericResult_ProducesASuccess()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void ImplicitConversion_FromErrorToGenericResult_ProducesAFailure()
    {
        Result<int> result = SampleError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void FirstFailureOrSuccess_WithNoFailures_ReturnsSuccess()
    {
        Result result = Result.FirstFailureOrSuccess(Result.Success(), Result.Success());

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void FirstFailureOrSuccess_ReturnsOnlyTheFirstFailure()
    {
        Error secondError = Error.Failure("Second.Error", "Second problem.");

        Result result = Result.FirstFailureOrSuccess(
            Result.Success(),
            Result.Failure(SampleError),
            Result.Failure(secondError));

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void AllOrValidationError_WithNoFailures_ReturnsSuccess()
    {
        Result result = Result.AllOrValidationError(Result.Success(), Result.Success());

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AllOrValidationError_CollectsEveryFailure()
    {
        Error secondError = Error.Failure("Second.Error", "Second problem.");

        Result result = Result.AllOrValidationError(
            Result.Success(),
            Result.Failure(SampleError),
            Result.Failure(secondError));

        result.IsFailure.ShouldBeTrue();
        ValidationError validationError = result.Error.ShouldBeOfType<ValidationError>();
        validationError.Errors.ShouldBe([SampleError, secondError]);
    }
}
