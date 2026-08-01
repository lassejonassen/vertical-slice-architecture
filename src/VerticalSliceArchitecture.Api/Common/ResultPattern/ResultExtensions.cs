namespace VerticalSliceArchitecture.Api.Common.ResultPattern;

public static class ResultExtensions
{
	// Shared across endpoints so each one doesn't have to re-derive a status code from
	// ErrorType itself.
	public static IResult ToProblem(this Result result)
	{
		if (result.IsSuccess)
		{
			throw new InvalidOperationException("A successful result cannot be converted to a problem response.");
		}

		var statusCode = result.Error.Type switch
		{
			ErrorType.Validation => StatusCodes.Status400BadRequest,
			ErrorType.NotFound => StatusCodes.Status404NotFound,
			ErrorType.Conflict => StatusCodes.Status409Conflict,
			_ => StatusCodes.Status400BadRequest,
		};

		return Results.Problem(
			statusCode: statusCode,
			title: result.Error.Code,
			detail: result.Error.Description,
			extensions: new Dictionary<string, object?>
			{
				["errorType"] = result.Error.Type.ToString(),
			});
	}
}
