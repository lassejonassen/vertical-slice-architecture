namespace VerticalSliceArchitecture.Api.Domain.Common;

public interface IDomainEvent
{
	DateTime OccurredOnUtc { get; }
}

public abstract record DomainEvent : IDomainEvent
{
	public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}