namespace VerticalSliceArchitecture.SharedKernel.Domain;

/// <summary>
/// Non-generic view over an aggregate's pending events.
/// <para>
/// Exists purely so infrastructure can collect events without knowing the ID type —
/// <c>OfType&lt;AggregateRoot&lt;TId&gt;&gt;()</c> is not expressible over a heterogeneous
/// change tracker, but <c>OfType&lt;IHasDomainEvents&gt;()</c> is.
/// </para>
/// </summary>
public interface IHasDomainEvents
{
    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    public void ClearDomainEvents();
}
