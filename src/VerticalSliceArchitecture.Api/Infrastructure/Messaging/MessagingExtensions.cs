using System.Reflection;
using VerticalSliceArchitecture.SharedKernel.Domain;

namespace VerticalSliceArchitecture.Api.Infrastructure.Messaging;

public static class MessagingExtensions
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>)
    ];

    /// <summary>
    /// Registers every handler in the assembly as scoped.
    /// <para>
    /// Registering them individually rather than only behind the dispatcher is what lets an endpoint
    /// inject <c>ICommandHandler&lt;T, TResponse&gt;</c> directly and skip the indirection entirely.
    /// </para>
    /// </summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services, Assembly assembly)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        IEnumerable<Type> concreteTypes = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (Type type in concreteTypes)
        {
            IEnumerable<Type> implemented = type
                .GetInterfaces()
                .Where(@interface => @interface.IsGenericType
                                     && HandlerInterfaces.Contains(@interface.GetGenericTypeDefinition()));

            foreach (Type @interface in implemented)
            {
                services.AddScoped(@interface, type);
            }
        }

        return services;
    }

    /// <summary>Registers every <c>IDomainEventHandler&lt;T&gt;</c> in the assembly.</summary>
    public static IServiceCollection AddDomainEventHandlers(
        this IServiceCollection services,
        Assembly assembly)
    {
        Type openHandler = typeof(IDomainEventHandler<>);

        IEnumerable<Type> concreteTypes = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (Type type in concreteTypes)
        {
            IEnumerable<Type> implemented = type
                .GetInterfaces()
                .Where(@interface => @interface.IsGenericType
                                     && @interface.GetGenericTypeDefinition() == openHandler);

            foreach (Type @interface in implemented)
            {
                // Several handlers may react to the same event, so this must be additive.
                services.AddScoped(@interface, type);
            }
        }

        return services;
    }
}
