using Microsoft.EntityFrameworkCore.Diagnostics;

namespace VerticalSliceArchitecture.Persistence.Interceptors;

/// <summary>
/// Collects domain events from tracked aggregates before the write and dispatches them after it
/// commits.
/// <para>
/// The ordering matters and is easy to get wrong. Events are harvested in <c>SavingChanges</c>
/// (while the change tracker still holds the aggregates) but published in <c>SavedChanges</c>
/// (once the data is durable). Publishing before the commit means handlers can act on a
/// transaction that then rolls back.
/// </para>
/// <para>
/// This is in-process and therefore at-most-once: if the process dies between commit and dispatch,
/// the events are lost. That is an acceptable trade for events that only drive local side effects.
/// The moment an event triggers something external — an email, a message on a bus, a call to SAP —
/// replace this with a transactional outbox. See the README.
/// </para>
/// </summary>
public sealed class DomainEventDispatchInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    private readonly List<IDomainEvent> _pendingEvents = [];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        HarvestEvents(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pendingEvents.Count > 0)
        {
            IDomainEvent[] events = [.. _pendingEvents];
            _pendingEvents.Clear();

            await dispatcher.DispatchAsync(events, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        // The write never happened, so the events describe something that did not occur.
        _pendingEvents.Clear();

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void HarvestEvents(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var roots = context.ChangeTracker
            .Entries()
            .Select(entry => entry.Entity)
            .OfType<IHasDomainEvents>()
            .Where(root => root.DomainEvents.Count > 0)
            .ToList();

        foreach (IHasDomainEvents root in roots)
        {
            _pendingEvents.AddRange(root.DomainEvents);
            root.ClearDomainEvents();
        }
    }
}
