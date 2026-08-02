using FluentValidation;
using FluentValidation.Results;

namespace VerticalSliceArchitecture.Api.Infrastructure.Endpoints.Filters;

/// <summary>
/// Runs FluentValidation against the request before the handler is reached.
/// <para>
/// This is shape validation only — required fields, lengths, ranges, well-formedness. Business
/// rules stay in the aggregate, where they belong and where they can be unit tested without a web
/// server. A rule that needs to look at other rows or at current state is not a validator.
/// </para>
/// <para>
/// A filter rather than a pipeline behaviour: it short-circuits before model binding hands off,
/// composes per route via <c>AddEndpointFilter</c>, and is visible on the endpoint declaration
/// instead of being applied invisibly to everything.
/// </para>
/// </summary>
public sealed class ValidationFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        IValidator<TRequest>? validator = context.HttpContext.RequestServices
            .GetService<IValidator<TRequest>>();

        if (validator is null)
        {
            return await next(context);
        }

        TRequest? request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is null)
        {
            return await next(context);
        }

        ValidationResult validation = await validator.ValidateAsync(
            request,
            context.HttpContext.RequestAborted);

        if (validation.IsValid)
        {
            return await next(context);
        }

        // ValidationProblem keys by property name, which is what a client binding a form wants;
        // domain-level validation errors go through ResultExtensions instead.
        return Results.ValidationProblem(
            validation.ToDictionary(),
            title: "Bad Request",
            extensions: new Dictionary<string, object?> { ["errorCode"] = "General.Validation" });
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class =>
        builder
            .AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesValidationProblem();
}
