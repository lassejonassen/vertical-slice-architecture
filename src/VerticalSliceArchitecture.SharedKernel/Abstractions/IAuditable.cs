namespace VerticalSliceArchitecture.SharedKernel.Abstractions;

/// <summary>
/// Implemented by persisted entities that should carry audit columns. Populated automatically by
/// <c>AuditableEntityInterceptor</c>, so aggregates never set these fields themselves.
/// </summary>
public interface IAuditable
{
    public DateTimeOffset CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
