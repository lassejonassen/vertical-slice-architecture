using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VerticalSliceArchitecture.Persistence;

/// <summary>
/// Resolves and invokes <see cref="IDomainEventHandler{TEvent}"/> implementations for each event.
/// <para>
/// A handler that throws is logged and swallowed rather than being allowed to bubble: by the time
/// this runs the transaction has already committed, so failing the request would report an error
/// for a write that actually succeeded. If a handler's failure genuinely must fail the operation,
/// it is not a domain event handler — call it from the command handler instead.
/// </para>
/// </summary>
public sealed class DomainEventDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        // A fresh scope: the originating DbContext is mid-SaveChanges and must not be reused.
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());

            foreach (object? handler in scope.ServiceProvider.GetServices(handlerType))
            {
                if (handler is null)
                {
                    continue;
                }

                try
                {
                    await (Task)handlerType
                        .GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!
                        .Invoke(handler, [domainEvent, cancellationToken])!;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Domain event handler {Handler} failed for {EventType} {EventId}",
                        handler.GetType().Name,
                        domainEvent.GetType().Name,
                        domainEvent.EventId);
                }
            }
        }
    }
}
