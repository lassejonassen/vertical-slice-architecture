using Shouldly;
using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.Domain.Users;

namespace VerticalSliceArchitecture.Domain.Tests.SharedKernel;

public class StronglyTypedIdTests
{
    [Fact]
    public void ClientId_New_GeneratesAUniqueVersion7Guid()
    {
        ClientId first = ClientId.New();
        ClientId second = ClientId.New();

        first.ShouldNotBe(second);
        first.Value.Version.ShouldBe(7);
    }

    [Fact]
    public void ClientId_From_RoundTripsTheGivenGuid()
    {
        Guid value = Guid.NewGuid();

        ClientId.From(value).Value.ShouldBe(value);
    }

    [Fact]
    public void ClientId_ToString_ReturnsTheGuidString()
    {
        Guid value = Guid.NewGuid();

        ClientId.From(value).ToString().ShouldBe(value.ToString());
    }

    [Fact]
    public void UserId_New_GeneratesAUniqueVersion7Guid()
    {
        UserId first = UserId.New();
        UserId second = UserId.New();

        first.ShouldNotBe(second);
        first.Value.Version.ShouldBe(7);
    }
}
