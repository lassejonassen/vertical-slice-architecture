namespace VerticalSliceArchitecture.SharedKernel.Abstractions;

/// <summary>
/// Injectable clock. The domain never reads <c>DateTimeOffset.UtcNow</c> itself — timestamps are
/// passed in as arguments so that aggregate behaviour is deterministic under test.
/// </summary>
public interface IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; }
}

/// <inheritdoc />
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
