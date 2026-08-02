namespace VerticalSliceArchitecture.Api.Infrastructure.RateLimiting;

public static class RateLimitPolicies
{
    /// <summary>Steady-state protection for ordinary reads and writes.</summary>
    public const string PerUser = nameof(PerUser);

    /// <summary>Tighter budget for expensive or abusable endpoints.</summary>
    public const string Sensitive = nameof(Sensitive);

    /// <summary>Bursty allowance for endpoints a UI may call in parallel.</summary>
    public const string Burst = nameof(Burst);
}
