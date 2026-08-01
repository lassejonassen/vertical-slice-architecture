using VerticalSliceArchitecture.Api.Domain.Common;

namespace VerticalSliceArchitecture.Api.Common.Messaging;

public class Mediator(IServiceProvider serviceProvider) : IMediator, IPublisher
{
	public async Task Send(IRequest request, CancellationToken cancellationToken = default)
	{
		var handlerType = typeof(IRequestHandler<>).MakeGenericType(request.GetType());
		dynamic handler = serviceProvider.GetRequiredService(handlerType);
		await handler.Handle((dynamic)request, cancellationToken);
	}

	public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
	{
		var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
		object? handler = serviceProvider.GetRequiredService(handlerType);

		// Get all pipeline behaviors
		var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType(), typeof(TResponse));
		var behaviors = serviceProvider.GetServices(behaviorType).Cast<dynamic>().ToList();

		// Final handler delegate
		RequestHandlerDelegate<TResponse> handlerDelegate = () => ((dynamic)handler).Handle((dynamic)request, cancellationToken);

		// Chain behaviors (in reverse)
		foreach (var behavior in behaviors.AsEnumerable().Reverse())
		{
			var next = handlerDelegate;
			handlerDelegate = () => behavior.Handle((dynamic)request, next, cancellationToken);
		}

		return await handlerDelegate();
	}

	public async Task Publish(IDomainEvent notification, CancellationToken cancellationToken = default)
	{
		var handlerType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
		var handlers = serviceProvider.GetServices(handlerType);

		foreach (dynamic handler in handlers)
		{
			await handler.Handle((dynamic)notification, cancellationToken);
		}
	}
}