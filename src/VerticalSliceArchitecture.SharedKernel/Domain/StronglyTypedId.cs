using System;
using System.Collections.Generic;
using System.Text;

namespace VerticalSliceArchitecture.SharedKernel.Domain;

/// <summary>
/// Marker for GUID-backed identity types. The static abstract members let generic code
/// (EF Core converters, model binders, test builders) create IDs without reflection.
/// </summary>
/// <remarks>
/// Implementations should be <c>readonly record struct</c> so equality, hashing and
/// <c>ToString</c> come for free and no allocation occurs.
/// <example>
/// <code>
/// public readonly record struct OrderId(Guid Value) : IStronglyTypedId&lt;OrderId&gt;
/// {
///     public static OrderId New() => new(Guid.CreateVersion7());
///     public static OrderId From(Guid value) => new(value);
///     public override string ToString() => Value.ToString();
/// }
/// </code>
/// </example>
/// </remarks>
public interface IStronglyTypedId<out TSelf> where TSelf : IStronglyTypedId<TSelf>
{
    public Guid Value { get; }

    /// <summary>Creates a new identity. Prefer UUIDv7 for index locality.</summary>
    public static abstract TSelf New();

    /// <summary>Rehydrates an identity from storage or from the transport layer.</summary>
    public static abstract TSelf From(Guid value);
}
