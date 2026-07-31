namespace VerticalSliceArchitecture.Api.Domain.Common;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
	where TId : notnull
{
	public TId Id { get; protected set; }

	protected Entity(TId id)
	{
		Id = id;
	}

	// EF Core requiring parameterless constructor
#pragma warning disable CS8618
	protected Entity() { }
#pragma warning restore CS8618

	public bool Equals(Entity<TId>? other)
	{
		if (other is null)
			return false;
		if (ReferenceEquals(this, other))
			return true;
		if (GetType() != other.GetType())
			return false;

		return EqualityComparer<TId>.Default.Equals(Id, other.Id);
	}

	public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

	public override int GetHashCode() => Id.GetHashCode() * 41;

	public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
		Equals(left, right);

	public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
		!Equals(left, right);
}