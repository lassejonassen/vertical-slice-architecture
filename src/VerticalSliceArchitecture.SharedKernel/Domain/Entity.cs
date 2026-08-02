namespace VerticalSliceArchitecture.SharedKernel.Domain;

/// <summary>
/// An object with a lifecycle and an identity. Two entities are equal when their IDs match,
/// regardless of their other state.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct
{
    protected Entity(TId id) => Id = id;

    /// <summary>Required by EF Core materialisation. Do not call from domain code.</summary>
    protected Entity()
    {
    }

    public TId Id { get; protected init; }

    public bool Equals(Entity<TId>? other) =>
        other is not null && other.GetType() == GetType() && other.Id.Equals(Id);

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
