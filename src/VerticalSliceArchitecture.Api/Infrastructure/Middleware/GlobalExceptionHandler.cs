using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace VerticalSliceArchitecture.Api.Infrastructure.Middleware;

/// <summary>
/// Last line of defence for failures the result pattern did not anticipate.
/// <para>
/// <see cref="IExceptionHandler"/> rather than a hand-written middleware: it participates in
/// <c>UseExceptionHandler</c> properly, several handlers can be chained, and it does not have to
/// re-implement the response-already-started checks that a custom middleware always gets wrong.
/// </para>
/// <para>
/// Anything reaching here is a bug or an infrastructure fault. Expected failures — validation,
/// missing rows, conflicts — never become exceptions in this codebase, which is what makes a 500
/// in the logs meaningful rather than routine.
/// </para>
/// </summary>
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>nginx's non-standard code for "client closed the connection"; not in StatusCodes.</summary>
    private const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // A cancelled request is the client hanging up, not a server fault. Logging these as
        // errors is the fastest way to make a 500 dashboard useless.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequestCancelled(logger, httpContext.Request.Method, httpContext.Request.Path);

            httpContext.Response.StatusCode = ClientClosedRequest;

            return true;
        }

        LogUnhandledException(logger, exception, httpContext.Request.Method, httpContext.Request.Path);

        (int statusCode, string title) = exception switch
        {
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad Request"),
            TimeoutException => (StatusCodes.Status504GatewayTimeout, "Gateway Timeout"),
            _ => (StatusCodes.Status500InternalServerError, "Server Failure")
        };

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = title,
            Status = statusCode,
            // Never leak exception detail outside development: messages routinely contain
            // connection strings, file paths and row data.
            Detail = environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred while processing the request."
        };

        problemDetails.Extensions["errorCode"] = "General.Unhandled";

        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Request {Method} {Path} was cancelled by the client")]
    private static partial void LogRequestCancelled(ILogger logger, string method, PathString path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string method, PathString path);
}
