using System.ComponentModel.DataAnnotations;

namespace VerticalSliceArchitecture.Persistence;

public enum DatabaseProvider
{
    PostgreSql = 0,
    SqlServer = 1
}

/// <summary>
/// Bound from the <c>Persistence</c> configuration section and validated at startup, so a bad
/// connection string fails the process rather than the first request that happens to need the database.
/// </summary>
public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public DatabaseProvider Provider { get; init; } = DatabaseProvider.PostgreSql;

    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Applies pending migrations on startup. Convenient locally, risky in production.</summary>
    public bool MigrateOnStartup { get; init; }

    /// <summary>Logs parameter values. Never enable outside development — parameters contain PII.</summary>
    public bool EnableSensitiveDataLogging { get; init; }

    [Range(0, 10)]
    public int MaxRetryCount { get; init; } = 3;

    [Range(1, 600)]
    public int CommandTimeoutSeconds { get; init; } = 30;
}
