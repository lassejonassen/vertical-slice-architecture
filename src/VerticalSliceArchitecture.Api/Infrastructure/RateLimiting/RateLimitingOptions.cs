using System.ComponentModel.DataAnnotations;

namespace VerticalSliceArchitecture.Api.Infrastructure.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; init; } = true;

    [Range(1, 100_000)]
    public int PerUserPermitLimit { get; init; } = 100;

    [Range(1, 3_600)]
    public int PerUserWindowSeconds { get; init; } = 60;

    [Range(0, 1_000)]
    public int PerUserQueueLimit { get; init; }

    [Range(1, 100_000)]
    public int SensitivePermitLimit { get; init; } = 10;

    [Range(1, 3_600)]
    public int SensitiveWindowSeconds { get; init; } = 60;

    [Range(1, 100_000)]
    public int BurstTokenLimit { get; init; } = 50;

    [Range(1, 100_000)]
    public int BurstTokensPerPeriod { get; init; } = 25;

    [Range(1, 3_600)]
    public int BurstReplenishmentPeriodSeconds { get; init; } = 10;
}
