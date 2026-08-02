using System;
using System.Collections.Generic;
using System.Text;

namespace VerticalSliceArchitecture.SharedKernel.Domain;

/// <summary>
/// Something that happened in the domain, expressed in past tense and in the ubiquitous language.
/// Raised by an aggregate, dispatched after the transaction commits.
/// </summary>
public interface IDomainEvent
{
    public Guid EventId { get; }

    public DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>Convenience base so concrete events stay one-liners.</summary>
public abstract record DomainEvent(DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}

/// <summary>
/// Handles a domain event. Handlers run after <c>SaveChangesAsync</c> succeeds, so they must not
/// assume they can roll the originating transaction back. If you need transactional guarantees,
/// swap the in-process dispatcher for the outbox (see README).
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>Publishes domain events to their registered handlers.</summary>
public interface IDomainEventDispatcher
{
    public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
