namespace VerticalSliceArchitecture.Api.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>
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