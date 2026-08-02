using Shouldly;
using VerticalSliceArchitecture.SharedKernel.Domain;

namespace VerticalSliceArchitecture.Domain.Tests.SharedKernel;

public class EntityTests
{
    [Fact]
    public void Equals_WithSameTypeAndId_ReturnsTrue()
    {
        Guid id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new TestEntity(id);

        first.Equals(second).ShouldBeTrue();
        (first == second).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ReturnsFalse()
    {
        var first = new TestEntity(Guid.NewGuid());
        var second = new TestEntity(Guid.NewGuid());

        first.Equals(second).ShouldBeFalse();
        (first != second).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentEntityTypeButSameId_ReturnsFalse()
    {
        Guid id = Guid.NewGuid();
        var entity = new TestEntity(id);
        var otherEntity = new OtherTestEntity(id);

        entity.Equals(otherEntity).ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_IsConsistentWithEquality()
    {
        Guid id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new TestEntity(id);

        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id);

    private sealed class OtherTestEntity(Guid id) : Entity<Guid>(id);
}
