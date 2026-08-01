namespace VerticalSliceArchitecture.Api.Domain.Common;

// Non-generic so the change tracker can query for any aggregate root regardless
// of its TId — generic class types aren't covariant (AggregateRoot<ProductId>
// is not an AggregateRoot<object>), so Entries<AggregateRoot<object>>() would
// never match a real entity.
public interface IHasDomainEvents
{
	IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

	void ClearDomainEvents();
}

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
	where TId : notnull
{
	private readonly List<IDomainEvent> _domainEvents = [];

	public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

	protected AggregateRoot(TId id) : base(id) { }

#pragma warning disable CS8618
	protected AggregateRoot() { }
#pragma warning restore CS8618

	/// <summary>
	/// Registers a domain event to be dispatched when the unit of work saves.
	/// </summary>
	protected void RaiseDomainEvent(IDomainEvent domainEvent)
	{
		_domainEvents.Add(domainEvent);
	}

	/// <summary>
	/// Clears raised domain events after they have been published.
	/// </summary>
	public void ClearDomainEvents()
	{
		_domainEvents.Clear();
	}
}