using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace VerticalSliceArchitecture.Api.Infrastructure.Endpoints;

public static class EndpointExtensions
{
    /// <summary>Discovers every <see cref="IEndpoint"/> in the assembly and registers it.</summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ServiceDescriptor[] descriptors = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                           && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(descriptors);

        return services;
    }

    /// <summary>
    /// Maps every discovered endpoint.
    /// <para>
    /// Pass a <paramref name="routeGroup"/> to apply shared conventions — a route prefix, an
    /// authorisation requirement, a rate limit — to all of them at once.
    /// </para>
    /// </summary>
    public static IApplicationBuilder MapEndpoints(
        this WebApplication app,
        RouteGroupBuilder? routeGroup = null)
    {
        IEnumerable<IEndpoint> endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

        IEndpointRouteBuilder builder = routeGroup is null ? app : routeGroup;

        foreach (IEndpoint endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }

        return app;
    }
}
