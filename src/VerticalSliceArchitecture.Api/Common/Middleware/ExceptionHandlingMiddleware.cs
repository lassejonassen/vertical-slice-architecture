using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace VerticalSliceArchitecture.Api.Common.Middleware;


internal sealed class ExceptionHandlingMiddleware(
	IProblemDetailsService problemDetailsService,
	ILogger<ExceptionHandlingMiddleware> logger) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		var correlationContext = httpContext.RequestServices.GetRequiredService<ICorrelationContext>();

		logger.LogError(
			exception,
			"Unhandled exception processing {Method} {Path}. CorrelationId: {CorrelationId}",
			httpContext.Request.Method,
			httpContext.Request.Path,
			correlationContext.CorrelationId);

		httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

		// Never surface the raw exception message/stack trace to the caller - only the log above
		// gets those. The correlation id lets support cross-reference this response with the log entry.
		return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
		{
			HttpContext = httpContext,
			Exception = exception,
			ProblemDetails = new ProblemDetails
			{
				Status = StatusCodes.Status500InternalServerError,
				Title = "An unexpected error occurred.",
				Detail = "Something went wrong on our end. Please try again, and contact support with the reference below if the problem persists.",
				Extensions =
				{
					["correlationId"] = correlationContext.CorrelationId,
				},
			},
		});
	}
}