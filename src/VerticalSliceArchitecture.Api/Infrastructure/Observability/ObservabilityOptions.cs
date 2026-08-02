using System.ComponentModel.DataAnnotations;

namespace VerticalSliceArchitecture.Api.Infrastructure.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    [Required(AllowEmptyStrings = false)]
    public string ServiceName { get; init; } = "acme-template-api";

    public string ServiceVersion { get; init; } = "0.0.0";

    /// <summary>OTLP endpoint. Empty disables export, which is what you want in unit tests.</summary>
    public string? OtlpEndpoint { get; init; }

    /// <summary>Either <c>grpc</c> (port 4317) or <c>httpprotobuf</c> (port 4318).</summary>
    public string OtlpProtocol { get; init; } = "grpc";

    /// <summary>Additional OTLP headers, e.g. an ingestion key. Supply via Key Vault, not appsettings.</summary>
    public string? OtlpHeaders { get; init; }

    /// <summary>Head sampling ratio. 1.0 keeps every trace; lower it once volume matters.</summary>
    [Range(0.0, 1.0)]
    public double TraceSampleRatio { get; init; } = 1.0;

    /// <summary>Emit logs to the console as JSON rather than as human-readable text.</summary>
    public bool UseJsonConsole { get; init; } = true;
}
