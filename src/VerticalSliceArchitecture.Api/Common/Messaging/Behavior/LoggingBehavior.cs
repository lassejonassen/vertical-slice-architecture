using System.Diagnostics;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Common.Messaging.Behavior;

internal sealed class LoggingBehavior<TRequest, TResponse>(
	ILogger<LoggingBehavior<TRequest, TResponse>> logger)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : class, IRequest<TResponse>
	where TResponse : Result
{
	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken)
	{
		string requestName = typeof(TRequest).Name;

		Activity.Current?.SetTag("request.name", requestName);

		logger.LogInformation("Processing request {RequestName}", requestName);

		TResponse result;
		try
		{
			result = await next();
		}
		catch (Exception ex)
		{
			Activity.Current?.SetTag("error", true);
			logger.LogError(ex, "Request {RequestName} threw an unhandled exception", requestName);
			throw;
		}

		if (result.IsSuccess)
		{
			logger.LogInformation("Completed request {RequestName}", requestName);
		}
		else
		{
			Activity.Current?.SetTag("error", true);
			logger.LogError("Completed request {RequestName} with error {@Error}", requestName, result.Error);
		}

		return result;

	}
}