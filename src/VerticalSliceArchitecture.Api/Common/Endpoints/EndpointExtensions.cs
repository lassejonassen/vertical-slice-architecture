using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace VerticalSliceArchitecture.Api.Common.Endpoints;

public static class EndpointExtensions
{
	/// <summary>
	/// Scans the specified assembly and registers all IEndpoint implementations as transient services.
	/// </summary>
	public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
	{
		var serviceDescriptors = assembly.GetTypes()
			.Where(t => t.IsAssignableTo(typeof(IEndpoint))
					 && t is { IsInterface: false, IsAbstract: false })
			.Select(t => ServiceDescriptor.Transient(typeof(IEndpoint), t));

		services.TryAddEnumerable(serviceDescriptors);

		return services;
	}

	/// <summary>
	/// Resolves all registered IEndpoint services from DI and invokes MapEndpoint on each.
	/// </summary>
	public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
	{
		var endpoints = app.ServiceProvider.GetServices<IEndpoint>();

		foreach (var endpoint in endpoints)
		{
			endpoint.MapEndpoint(app);
		}

		return app;
	}
}