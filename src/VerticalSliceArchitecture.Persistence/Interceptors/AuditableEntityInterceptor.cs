using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace VerticalSliceArchitecture.Persistence.Interceptors;

/// <summary>
/// Stamps created/modified metadata on anything implementing <see cref="IAuditable"/>.
/// <para>
/// An interceptor rather than a base-class hook: aggregates should not know that auditing exists,
/// and this way a new aggregate is audited the moment it implements the interface.
/// </para>
/// </summary>
public sealed class AuditableEntityInterceptor(
    IDateTimeProvider dateTimeProvider,
    ICurrentUserAccessor currentUserAccessor) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTimeOffset nowUtc = dateTimeProvider.UtcNow;
        string? actor = currentUserAccessor.UserName;

        foreach (EntityEntry<IAuditable> entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOnUtc = nowUtc;
                    entry.Entity.CreatedBy = actor;
                    break;

                // HasChangedOwnedEntities catches the case where only an owned value object changed;
                // EF reports the parent as Unchanged there, which would otherwise skip the stamp.
                case EntityState.Modified:
                case EntityState.Unchanged when HasChangedOwnedEntities(entry):
                    entry.Entity.ModifiedOnUtc = nowUtc;
                    entry.Entity.ModifiedBy = actor;
                    break;
            }
        }
    }

    private static bool HasChangedOwnedEntities(EntityEntry entry) =>
        entry.References.Any(reference =>
            reference.TargetEntry is { } target
            && target.Metadata.IsOwned()
            && target.State is EntityState.Added or EntityState.Modified);
}

/// <summary>
/// Supplies the acting principal to persistence-level concerns. Implemented in the API project,
/// where <c>HttpContext</c> is available; the persistence layer only needs the name.
/// </summary>
public interface ICurrentUserAccessor
{
    public string? UserName { get; }
}
