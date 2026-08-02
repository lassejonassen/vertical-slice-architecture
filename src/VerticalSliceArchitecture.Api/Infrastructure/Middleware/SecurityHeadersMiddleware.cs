namespace VerticalSliceArchitecture.Api.Infrastructure.Middleware;

/// <summary>
/// Applies the response headers that make sense for a JSON API.
/// <para>
/// Deliberately not a full browser policy: there is no CSP or HSTS here, because this service
/// serves no HTML and terminating TLS is the ingress's job. Adding headers that do nothing for
/// the actual threat model is noise that later gets copied somewhere it matters.
/// </para>
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            IHeaderDictionary headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Frame-Options"] = "DENY";
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            return Task.CompletedTask;
        });

        return next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
