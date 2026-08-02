using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace VerticalSliceArchitecture.Api.Infrastructure.Observability;

public static class SerilogExtensions
{
    /// <summary>
    /// A logger available before the host is built, so that a failure during configuration or DI
    /// validation is logged rather than lost. Replaced by the fully configured logger later.
    /// </summary>
    public static void CreateBootstrapLogger() =>
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

    /// <summary>
    /// Wires Serilog as the logging provider.
    /// <para>
    /// Serilog handles logs; OpenTelemetry handles traces and metrics. That split is deliberate —
    /// Serilog's structured API and enrichment are better than the raw <c>ILogger</c> experience,
    /// and its OTLP sink means the logs still land in the same backend as the traces. Correlation
    /// works because the trace and span IDs are enriched onto every event below.
    /// </para>
    /// </summary>
    public static IHostBuilder UseApplicationSerilog(this ConfigureHostBuilder host) =>
        host.UseSerilog((context, serviceProvider, configuration) =>
        {
            ObservabilityOptions options = context.Configuration
                .GetSection(ObservabilityOptions.SectionName)
                .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                // Without these two, a log line cannot be tied back to the trace it belongs to.
                .Enrich.WithSpan()
                .Enrich.WithProperty("service.name", options.ServiceName)
                .Enrich.WithProperty("service.version", options.ServiceVersion);

            if (options.UseJsonConsole)
            {
                configuration.WriteTo.Console(new CompactJsonFormatter());
            }
            else
            {
                configuration.WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}");
            }

            if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
            {
                configuration.WriteTo.OpenTelemetry(sink =>
                {
                    sink.Endpoint = options.OtlpEndpoint;
                    sink.Protocol = options.OtlpProtocol.Equals("grpc", StringComparison.OrdinalIgnoreCase)
                        ? Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc
                        : Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf;
                    sink.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = options.ServiceName,
                        ["service.version"] = options.ServiceVersion,
                        ["deployment.environment"] = context.HostingEnvironment.EnvironmentName
                    };
                });
            }
        });

    /// <summary>
    /// One summary line per request instead of the framework's several, with useful properties
    /// attached and health checks dropped to Verbose so they do not drown everything else.
    /// </summary>
    public static IApplicationBuilder UseApplicationRequestLogging(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(logging =>
        {
            logging.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            logging.GetLevel = (httpContext, elapsed, exception) => exception is not null
                ? LogEventLevel.Error
                : httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError
                    ? LogEventLevel.Error
                    : IsHealthCheck(httpContext)
                        ? LogEventLevel.Verbose
                        : elapsed > 2_000
                            ? LogEventLevel.Warning
                            : LogEventLevel.Information;

            logging.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());

                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserName", httpContext.User.Identity.Name);
                }

                if (httpContext.GetEndpoint() is { DisplayName: { } endpointName })
                {
                    diagnosticContext.Set("Endpoint", endpointName);
                }
            };
        });

    private static bool IsHealthCheck(HttpContext httpContext) =>
        httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
        || httpContext.Request.Path.StartsWithSegments("/alive", StringComparison.OrdinalIgnoreCase);
}
