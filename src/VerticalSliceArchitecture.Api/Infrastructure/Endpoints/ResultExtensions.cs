using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Infrastructure.Endpoints;

/// <summary>
/// The single place where a domain <see cref="Error"/> becomes an HTTP status code.
/// <para>
/// Centralising this is the main practical payoff of the result pattern. Handlers never mention
/// status codes, so a new failure mode is one entry in an error catalog rather than an audit of
/// every endpoint. Output follows RFC 9457 (Problem Details), with the domain error code carried
/// in an <c>errorCode</c> extension member so clients can branch on something stable rather than
/// parsing prose.
/// </para>
/// </summary>
public static class ResultExtensions
{
    public static IResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be converted to a problem.");
        }

        return BuildProblem(result.Error);
    }

    /// <summary>Maps a result to a response, delegating only the success shape to the caller.</summary>
    public static IResult Match<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : BuildProblem(result.Error);

    public static IResult Match(this Result result, Func<IResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : BuildProblem(result.Error);

    /// <summary>Success becomes <c>200 OK</c> with the value as the body.</summary>
    public static IResult ToOk<TValue>(this Result<TValue> result) =>
        result.Match(Results.Ok);

    /// <summary>Success becomes <c>204 No Content</c>.</summary>
    public static IResult ToNoContent(this Result result) =>
        result.Match(Results.NoContent);

    /// <summary>Success becomes <c>201 Created</c> with a Location header.</summary>
    public static IResult ToCreated<TValue>(this Result<TValue> result, Func<TValue, string> locationFactory) =>
        result.Match(value => Results.Created(locationFactory(value), value));

    private static IResult BuildProblem(Error error)
    {
        var extensions = new Dictionary<string, object?> { ["errorCode"] = error.Code };

        // Validation failures are reported as a flat list rather than the MVC-style
        // field-keyed dictionary, because domain errors are not bound to request fields.
        if (error is ValidationError validationError)
        {
            extensions["errors"] = validationError.Errors
                .Select(inner => new { code = inner.Code, description = inner.Description })
                .ToArray();
        }

        return Results.Problem(
            title: GetTitle(error.Type),
            detail: error.Description,
            statusCode: GetStatusCode(error.Type),
            type: GetRfcType(error.Type),
            extensions: extensions);
    }

    private static int GetStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string GetTitle(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "Bad Request",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.NotFound => "Not Found",
        ErrorType.Conflict => "Conflict",
        _ => "Server Failure"
    };

    private static string GetRfcType(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        ErrorType.Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        ErrorType.NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        ErrorType.Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };
}
