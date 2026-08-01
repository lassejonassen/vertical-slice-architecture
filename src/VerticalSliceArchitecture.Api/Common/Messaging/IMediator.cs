using VerticalSliceArchitecture.Api.Domain.Common;

namespace VerticalSliceArchitecture.Api.Common.Messaging;

public interface IMediator
{
	Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
	Task Send(IRequest request, CancellationToken cancellationToken = default);
}

public interface IPublisher
{
	Task Publish(IDomainEvent notification, CancellationToken cancellationToken = default);
}

public interface INotificationHandler<in TNotification>
	where TNotification : IDomainEvent
{
	Task Handle(TNotification notification, CancellationToken cancellationToken);
}

public interface IRequest<out TResponse> { }

public interface IRequest : IRequest<Unit> { }

public interface IRequestHandler<in TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestHandler<in TRequest>
	where TRequest : IRequest
{
	Task<Unit> Handle(TRequest request, CancellationToken cancellationToken);
}

public readonly struct Unit
{
	public static readonly Unit Value = new();
}
