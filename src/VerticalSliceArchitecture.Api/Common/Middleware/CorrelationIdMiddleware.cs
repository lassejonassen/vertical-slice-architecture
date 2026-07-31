using Serilog.Context;
using System.Diagnostics;

namespace VerticalSliceArchitecture.Api.Common.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
	private const string CorrelationIdHeader = "X-Correlation-ID";

	public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
	{
		// Try to find the header from the caller
		if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var extractedId) &&
			Guid.TryParse(extractedId, out var guid))
		{
			// Cast to the setter interface (Infrastructure only)
			if (correlationContext is ICorrelationIdSetter setter)
			{
				setter.Set(guid);
			}
		}

		// Attach the ID to the response headers for the caller's benefit
		context.Response.OnStarting(() =>
		{
			context.Response.Headers[CorrelationIdHeader] = correlationContext.CorrelationId.ToString();
			return Task.CompletedTask;
		});

		// Lets a support engineer pivot from a logged CorrelationId to the matching trace/span -
		// without this, the two observability signals aren't joinable.
		Activity.Current?.SetTag("correlation.id", correlationContext.CorrelationId.ToString());

		using (LogContext.PushProperty("CorrelationId", correlationContext.CorrelationId))
		{
			await next(context);
		}
	}
}