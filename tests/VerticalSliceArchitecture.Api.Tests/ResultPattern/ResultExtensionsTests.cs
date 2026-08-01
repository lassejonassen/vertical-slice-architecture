using Microsoft.AspNetCore.Http.HttpResults;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Tests.ResultPattern;

public class ResultExtensionsTests
{
	[Fact]
	public void ToProblem_OnSuccess_Throws()
	{
		var result = Result.Success();

		var act = result.ToProblem;

		Assert.Throws<InvalidOperationException>(act);
	}

	[Theory]
	[InlineData(ErrorType.Validation, 400)]
	[InlineData(ErrorType.NotFound, 404)]
	[InlineData(ErrorType.Conflict, 409)]
	[InlineData(ErrorType.Failure, 400)]
	public void ToProblem_OnFailure_MapsErrorTypeToExpectedStatusCode(ErrorType errorType, int expectedStatusCode)
	{
		var result = Result.Failure(new Error("Some.Error", "Description.", errorType));

		var problem = Assert.IsType<ProblemHttpResult>(result.ToProblem());

		Assert.Equal(expectedStatusCode, problem.StatusCode);
		Assert.Equal("Some.Error", problem.ProblemDetails.Title);
		Assert.Equal("Description.", problem.ProblemDetails.Detail);
	}
}
