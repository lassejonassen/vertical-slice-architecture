using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Tests.ResultPattern;

public class ResultTests
{
	private static readonly Error SampleError = new("Sample.Error", "Something went wrong.", ErrorType.Failure);

	[Fact]
	public void Success_IsSuccessWithNoError()
	{
		var result = Result.Success();

		Assert.True(result.IsSuccess);
		Assert.False(result.IsFailure);
		Assert.Equal(Error.None, result.Error);
	}

	[Fact]
	public void Failure_IsFailureWithGivenError()
	{
		var result = Result.Failure(SampleError);

		Assert.False(result.IsSuccess);
		Assert.True(result.IsFailure);
		Assert.Equal(SampleError, result.Error);
	}

	[Fact]
	public void Constructor_SuccessWithNonNoneError_Throws()
	{
		var act = () => new Result(true, SampleError);

		Assert.Throws<InvalidOperationException>(act);
	}

	[Fact]
	public void Constructor_FailureWithNoneError_Throws()
	{
		var act = () => new Result(false, Error.None);

		Assert.Throws<InvalidOperationException>(act);
	}

	[Fact]
	public void ResultOfT_ImplicitConversion_FromValue_IsSuccess()
	{
		Result<string> result = "value";

		Assert.True(result.IsSuccess);
		Assert.Equal("value", result.Value);
	}

	[Fact]
	public void ResultOfT_ImplicitConversion_FromNull_IsFailureWithNullValueError()
	{
		Result<string> result = (string)null!;

		Assert.True(result.IsFailure);
		Assert.Equal(Error.NullValue, result.Error);
	}
}
