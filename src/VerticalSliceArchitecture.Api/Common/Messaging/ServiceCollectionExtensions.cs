using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using VerticalSliceArchitecture.Api.Common.Messaging.Behavior;

namespace VerticalSliceArchitecture.Api.Common.Messaging;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddMediator(this IServiceCollection services)
	{
		services.TryAddScoped<IMediator, Mediator>();

		// Register pipeline behaviors as enumerable so multiple implementations are included.
		services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)));

		return services;
	}

	public static IServiceCollection AddMediatorHandlers(this IServiceCollection services, params Assembly[] assemblies)
	{
		services.Scan(scan => scan
		.FromAssemblies(assemblies)
		.AddClasses(c => c.AssignableTo(typeof(IRequestHandler<,>)))
		.AsImplementedInterfaces()
		.WithScopedLifetime()
		.AddClasses(c => c.AssignableTo(typeof(IRequestHandler<>)))
		.AsImplementedInterfaces()
		.WithScopedLifetime());

		return services;
	}
}