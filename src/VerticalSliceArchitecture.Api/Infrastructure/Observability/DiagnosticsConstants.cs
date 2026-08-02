using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace VerticalSliceArchitecture.Api.Infrastructure.Observability;

/// <summary>
/// The application's own instrumentation. Named once here so the names cannot drift between the
/// code that emits and the registration that subscribes — a mismatch there produces silence rather
/// than an error, which is a miserable thing to debug.
/// </summary>
public static class DiagnosticsConstants
{
    public const string ActivitySourceName = "Acme.Template";
    public const string MeterName = "Acme.Template";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static readonly Meter Meter = new(MeterName);

    /// <summary>Example domain metric. Business counters age far better than request counts.</summary>
    public static readonly Counter<long> ClientsRegistered =
        Meter.CreateCounter<long>(
            "acme.clients.registered",
            unit: "{client}",
            description: "Number of clients successfully registered.");
}
