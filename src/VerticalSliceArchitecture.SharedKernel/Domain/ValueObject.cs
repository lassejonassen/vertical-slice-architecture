namespace VerticalSliceArchitecture.SharedKernel.Domain;

/// <summary>
/// Base class for value objects that need custom equality semantics — for example ignoring case,
/// or comparing a normalised form rather than the raw input.
/// <para>
/// For the common case prefer <c>readonly record struct</c> or <c>sealed record</c>: structural
/// equality is generated for you and there is nothing to get wrong. This base exists for the
/// minority of cases where that default is not what you want.
/// </para>
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Yields the components that participate in equality, in a stable order.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other) =>
        other is not null
        && other.GetType() == GetType()
        && other.GetEqualityComponents().SequenceEqual(GetEqualityComponents());

    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(new HashCode(), (hash, component) =>
        {
            hash.Add(component);
            return hash;
        }).ToHashCode();

    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
