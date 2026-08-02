using System.Diagnostics;
using Serilog.Context;

namespace VerticalSliceArchitecture.Api.Infrastructure.Middleware;

/// <summary>
/// Establishes a correlation ID for the request, echoes it back, and pushes it onto the Serilog
/// context so every log line written downstream carries it.
/// <para>
/// The W3C trace ID is preferred over a freshly generated GUID, because it is the same value
/// OpenTelemetry puts on the spans — which means a correlation ID from a support ticket leads
/// directly to the trace, rather than to a second identifier that has to be joined manually.
/// </para>
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = ResolveCorrelationId(context);

        context.TraceIdentifier = correlationId;

        // Set on OnStarting rather than directly: writing to Headers after the response has begun
        // throws, and any downstream component may start it.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;

            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out Microsoft.Extensions.Primitives.StringValues incoming)
            && !string.IsNullOrWhiteSpace(incoming))
        {
            // Cap the length: this value ends up in logs and response headers, and an unbounded
            // caller-supplied string is a cheap way to pollute both.
            string value = incoming.ToString();

            return value.Length <= 128 ? value : value[..128];
        }

        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }
}

public static class CorrelationIdExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
