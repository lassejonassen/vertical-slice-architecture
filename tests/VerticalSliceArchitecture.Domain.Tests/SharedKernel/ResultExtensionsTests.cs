using Shouldly;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Tests.SharedKernel;

public class ResultExtensionsTests
{
    private static readonly Error SampleError = Error.Failure("Sample.Error", "Something went wrong.");

    [Fact]
    public void Map_OnSuccess_ProjectsTheValue()
    {
        Result<int> result = Result.Success(2).Map(value => value * 2);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(4);
    }

    [Fact]
    public void Map_OnFailure_PassesTheErrorThrough()
    {
        Result<int> result = Result.Failure<int>(SampleError).Map(value => value * 2);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void Bind_OnSuccess_ChainsTheNextResult()
    {
        Result<string> result = Result.Success(2).Bind(value => Result.Success(value.ToString()));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("2");
    }

    [Fact]
    public void Bind_OnFailure_ShortCircuitsWithoutCallingTheNextStep()
    {
        var called = false;

        Result<string> result = Result.Failure<int>(SampleError).Bind(value =>
        {
            called = true;

            return Result.Success(value.ToString());
        });

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task BindAsync_OnSuccess_ChainsTheNextAsyncResult()
    {
        Result<string> result =
            await Result.Success(2).BindAsync(value => Task.FromResult(Result.Success(value.ToString())));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("2");
    }

    [Fact]
    public void Tap_OnSuccess_RunsTheSideEffectAndReturnsTheOriginalResult()
    {
        var seen = 0;

        Result<int> result = Result.Success(5).Tap(value => seen = value);

        result.Value.ShouldBe(5);
        seen.ShouldBe(5);
    }

    [Fact]
    public void Tap_OnFailure_DoesNotRunTheSideEffect()
    {
        var called = false;

        Result.Failure<int>(SampleError).Tap(_ => called = true);

        called.ShouldBeFalse();
    }

    [Fact]
    public void Match_Generic_InvokesTheCorrespondingBranch()
    {
        string success = Result.Success(1).Match(_ => "ok", _ => "fail");
        string failure = Result.Failure<int>(SampleError).Match(_ => "ok", _ => "fail");

        success.ShouldBe("ok");
        failure.ShouldBe("fail");
    }

    [Fact]
    public void Match_NonGeneric_InvokesTheCorrespondingBranch()
    {
        string success = Result.Success().Match(() => "ok", _ => "fail");
        string failure = Result.Failure(SampleError).Match(() => "ok", _ => "fail");

        success.ShouldBe("ok");
        failure.ShouldBe("fail");
    }

    [Fact]
    public void ToResult_WithNonNullValue_ReturnsSuccess()
    {
        Result<string> result = "value".ToResult(SampleError);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("value");
    }

    [Fact]
    public void ToResult_WithNullValue_ReturnsFailure()
    {
        string? value = null;

        Result<string> result = value.ToResult(SampleError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }
}
