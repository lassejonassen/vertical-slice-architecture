using FluentValidation;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Common.Messaging.Behavior;

internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : class, IRequest<TResponse>
	where TResponse : Result
{
	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken)
	{
		if (!validators.Any())
		{
			return await next();
		}

		var context = new ValidationContext<TRequest>(request);

		var failures = (await Task.WhenAll(
				validators.Select(validator => validator.ValidateAsync(context, cancellationToken))))
			.SelectMany(result => result.Errors)
			.ToList();

		if (failures.Count == 0)
		{
			return await next();
		}

		var error = new Error(
			$"Validation.{typeof(TRequest).Name}",
			string.Join(" | ", failures.Select(f => f.ErrorMessage).Distinct()),
			ErrorType.Validation);

		return CreateValidationFailure(error);
	}

	// TResponse is either `Result` or `Result<TValue>` (enforced by the generic constraint),
	// but which one isn't known until runtime, so the matching Failure(...) overload has to be
	// picked via reflection.
	private static TResponse CreateValidationFailure(Error error)
	{
		if (typeof(TResponse) == typeof(Result))
		{
			return (TResponse)(object)Result.Failure(error);
		}

		var valueType = typeof(TResponse).GetGenericArguments()[0];

		var failureMethod = typeof(Result)
			.GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
			.MakeGenericMethod(valueType);

		return (TResponse)failureMethod.Invoke(null, [error])!;
	}
}
