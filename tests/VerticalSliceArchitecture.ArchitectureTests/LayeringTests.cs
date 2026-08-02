using NetArchTest.Rules;
using Shouldly;
using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.Integrations;
using VerticalSliceArchitecture.Persistence;
using VerticalSliceArchitecture.SharedKernel.Results;
using TestResult = NetArchTest.Rules.TestResult;

namespace VerticalSliceArchitecture.ArchitectureTests;

/// <summary>
/// Enforces the dependency direction the vertical-slice layering relies on:
/// <c>SharedKernel &lt;- Domain &lt;- Persistence &lt;- Api</c>, with <c>Integrations</c> reserved
/// for future external-system adapters. A slice can only reach inward to a lower layer; nothing
/// downstream should ever need to know a higher layer exists.
/// </summary>
public class LayeringTests
{
    [Fact]
    public void SharedKernel_ShouldNotDependOnAnyOtherProject()
    {
        TestResult result = Types.InAssembly(typeof(Error).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "VerticalSliceArchitecture.Domain",
                "VerticalSliceArchitecture.Persistence",
                "VerticalSliceArchitecture.Api",
                "VerticalSliceArchitecture.Integrations")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureReasons(result));
    }

    [Fact]
    public void Domain_ShouldNotDependOnPersistenceApiOrIntegrations()
    {
        TestResult result = Types.InAssembly(typeof(Client).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "VerticalSliceArchitecture.Persistence",
                "VerticalSliceArchitecture.Api",
                "VerticalSliceArchitecture.Integrations")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureReasons(result));
    }

    [Fact]
    public void Domain_ShouldNotDependOnEntityFrameworkCoreOrAspNetCore()
    {
        TestResult result = Types.InAssembly(typeof(Client).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureReasons(result));
    }

    [Fact]
    public void Persistence_ShouldNotDependOnApiOrIntegrations()
    {
        TestResult result = Types.InAssembly(typeof(ApplicationDbContext).Assembly)
            .Should()
            .NotHaveDependencyOnAny("VerticalSliceArchitecture.Api", "VerticalSliceArchitecture.Integrations")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureReasons(result));
    }

    [Fact]
    public void Persistence_ShouldNotDependOnAspNetCore()
    {
        // Persistence is infrastructure for data access only; anything HTTP-shaped belongs in Api.
        TestResult result = Types.InAssembly(typeof(ApplicationDbContext).Assembly)
            .Should()
            .NotHaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureReasons(result));
    }

    [Fact]
    public void Integrations_ShouldNotDependOnApi()
    {
        // Guards the direction even though nothing in Integrations exists yet: adapters for
        // external systems should be callable from Api, never the other way around.
        TestResult result = Types.InAssembly(typeof(Class1).Assembly)
            .Should()
            .NotHaveDependencyOn("VerticalSliceArchitecture.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureReasons(result));
    }

    internal static string FailureReasons(TestResult result) =>
        result.FailingTypeNames is null
            ? "No further detail available."
            : string.Join(", ", result.FailingTypeNames);
}
