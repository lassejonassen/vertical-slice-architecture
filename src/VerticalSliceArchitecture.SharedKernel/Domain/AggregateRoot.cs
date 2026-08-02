namespace VerticalSliceArchitecture.SharedKernel.Domain;

/// <summary>
/// The consistency boundary. Everything outside the aggregate references it only by ID, and
/// every invariant inside it holds at the end of each public method.
/// <para>
/// Repositories are defined per aggregate root, never per entity.
/// </para>
/// <para>
/// There is deliberately no <c>Version</c> property here. Optimistic concurrency is a persistence
/// concern and the two supported providers disagree on its shape — PostgreSQL uses the <c>xmin</c>
/// system column (uint), SQL Server uses <c>rowversion</c> (byte[]). Modelling it as a shadow
/// property keeps that difference out of the domain entirely.
/// </para>
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : struct
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>Required by EF Core materialisation. Do not call from domain code.</summary>
    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Called by the persistence layer once events have been handed to the dispatcher.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
