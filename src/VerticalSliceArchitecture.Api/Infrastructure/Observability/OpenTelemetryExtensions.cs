using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace VerticalSliceArchitecture.Api.Infrastructure.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddApplicationObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<ObservabilityOptions>()
            .Bind(configuration.GetSection(ObservabilityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        ObservabilityOptions options = configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        // W3C trace context, so a trace started in the Angular client survives the hop into the API.
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(options.ServiceName, serviceVersion: options.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment.EnvironmentName,
                    ["host.name"] = Environment.MachineName
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.TraceSampleRatio)))
                    .AddSource(DiagnosticsConstants.ActivitySourceName)
                    .AddAspNetCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.RecordException = true;
                        // Health probes are high-volume and carry no information.
                        instrumentation.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/health")
                            && !httpContext.Request.Path.StartsWithSegments("/alive");
                    })
                    .AddHttpClientInstrumentation(instrumentation => instrumentation.RecordException = true)
                    .AddEntityFrameworkCoreInstrumentation(instrumentation =>
                    {
                        // Statement text only, never parameter values - those contain PII.
                        instrumentation.SetDbStatementForText = true;
                        instrumentation.SetDbStatementForStoredProcedure = true;
                    });

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporter => ConfigureOtlp(exporter, options));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(DiagnosticsConstants.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation();

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(exporter => ConfigureOtlp(exporter, options));
                }
            });

        return services;
    }

    private static void ConfigureOtlp(
        OpenTelemetry.Exporter.OtlpExporterOptions exporter,
        ObservabilityOptions options)
    {
        exporter.Endpoint = new Uri(options.OtlpEndpoint!);

        exporter.Protocol = options.OtlpProtocol.Equals("grpc", StringComparison.OrdinalIgnoreCase)
            ? OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
            : OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;

        if (!string.IsNullOrWhiteSpace(options.OtlpHeaders))
        {
            exporter.Headers = options.OtlpHeaders;
        }
    }
}
