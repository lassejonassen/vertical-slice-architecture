using NetArchTest.Rules;
using Shouldly;
using VerticalSliceArchitecture.Api.Infrastructure.Endpoints;
using VerticalSliceArchitecture.Api.Infrastructure.Messaging;
using VerticalSliceArchitecture.SharedKernel.Domain;
using TestResult = NetArchTest.Rules.TestResult;

namespace VerticalSliceArchitecture.ArchitectureTests;

/// <summary>
/// Enforces the shape every vertical slice is expected to follow, so a new slice added without
/// reading the others still lands on the same conventions: endpoints and handlers are sealed and
/// internal (no reason for anything outside DI/assembly-scanning to reach them directly), and they
/// live under <c>Features</c> rather than being scattered across the assembly.
/// </summary>
public class FeatureConventionTests
{
    private static readonly System.Reflection.Assembly ApiAssembly = typeof(IEndpoint).Assembly;

    [Fact]
    public void Endpoints_ShouldBeSealed()
    {
        TestResult result = Types.InAssembly(ApiAssembly)
            .That().ImplementInterface(typeof(IEndpoint))
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(LayeringTests.FailureReasons(result));
    }

    [Fact]
    public void Endpoints_ShouldNotBePublic()
    {
        TestResult result = Types.InAssembly(ApiAssembly)
            .That().ImplementInterface(typeof(IEndpoint))
            .Should().NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(LayeringTests.FailureReasons(result));
    }

    [Fact]
    public void Endpoints_ShouldResideUnderFeatures()
    {
        TestResult result = Types.InAssembly(ApiAssembly)
            .That().ImplementInterface(typeof(IEndpoint))
            .Should().ResideInNamespaceStartingWith("VerticalSliceArchitecture.Api.Features")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(LayeringTests.FailureReasons(result));
    }

    [Fact]
    public void Endpoints_ShouldHaveNameEndingWithEndpoint()
    {
        TestResult result = Types.InAssembly(ApiAssembly)
            .That().ImplementInterface(typeof(IEndpoint))
            .Should().HaveNameEndingWith("Endpoint")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(LayeringTests.FailureReasons(result));
    }

    [Fact]
    public void CommandAndQueryHandlers_ShouldBeSealed()
    {
        TestResult result = Types.InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or().ImplementInterface(typeof(ICommandHandler<,>))
            .Or().ImplementInterface(typeof(IQueryHandler<,>))
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(LayeringTests.FailureReasons(result));
    }

    [Fact]
    public void CommandAndQueryHandlers_ShouldNotBePublic()
    {
        TestResult result = Types.InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or().ImplementInterface(typeof(ICommandHandler<,>))
            .Or().ImplementInterface(typeof(IQueryHandler<,>))
            .Should().NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(LayeringTests.FailureReasons(result));
    }

    [Fact]
    public void CommandAndQueryHandlers_ShouldHaveNameEndingWithHandler()
    {
        TestResult result = Types.InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or().ImplementInterface(typeof(ICommandHandler<,>))
            .Or().ImplementInterface(typeof(IQueryHandler<,>))
            .Should().HaveNameEndingWith("Handler")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(LayeringTests.FailureReasons(result));
    }

    [Fact]
    public void DomainEventHandlers_ShouldBeSealedAndNotPublic()
    {
        TestResult result = Types.InAssembly(ApiAssembly)
            .That().ImplementInterface(typeof(IDomainEventHandler<>))
            .Should().BeSealed()
            .And().NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(LayeringTests.FailureReasons(result));
    }
}
