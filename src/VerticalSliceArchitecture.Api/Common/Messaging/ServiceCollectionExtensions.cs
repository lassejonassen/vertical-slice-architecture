using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using VerticalSliceArchitecture.Api.Common.Messaging.Behavior;

namespace VerticalSliceArchitecture.Api.Common.Messaging;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddMediator(this IServiceCollection services)
	{
		services.TryAddScoped<Mediator>();
		services.TryAddScoped<IMediator>(sp => sp.GetRequiredService<Mediator>());
		services.TryAddScoped<IPublisher>(sp => sp.GetRequiredService<Mediator>());

		// Register pipeline behaviors as enumerable so multiple implementations are included.
		// Order matters: behaviors run in registration order, outermost first, so logging wraps
		// validation, and validation runs before the handler is ever reached.
		services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)));
		services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));

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
		.WithScopedLifetime()
		.AddClasses(c => c.AssignableTo(typeof(INotificationHandler<>)))
		.AsImplementedInterfaces()
		.WithScopedLifetime()
		.AddClasses(c => c.AssignableTo(typeof(IValidator<>)))
		.AsImplementedInterfaces()
		.WithScopedLifetime());

		return services;
	}
}