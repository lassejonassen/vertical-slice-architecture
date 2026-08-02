using Shouldly;
using VerticalSliceArchitecture.SharedKernel.Domain;

namespace VerticalSliceArchitecture.Domain.Tests.SharedKernel;

public class AggregateRootTests
{
    [Fact]
    public void Raise_AddsTheEventToDomainEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(DateTimeOffset.UtcNow);

        aggregate.RaiseEvent(domainEvent);

        aggregate.DomainEvents.ShouldHaveSingleItem().ShouldBe(domainEvent);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheCollection()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestDomainEvent(DateTimeOffset.UtcNow));

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    private sealed record TestDomainEvent(DateTimeOffset OccurredOnUtc) : DomainEvent(OccurredOnUtc);

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void RaiseEvent(IDomainEvent domainEvent) => Raise(domainEvent);
    }
}
