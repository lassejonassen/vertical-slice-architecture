namespace VerticalSliceArchitecture.SharedKernel.Abstractions;

/// <summary>
/// Commits the current transaction. Exposed to feature handlers so they never need to see
/// <c>DbContext</c> directly, which keeps the persistence choice swappable and the slice testable.
/// </summary>
public interface IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
