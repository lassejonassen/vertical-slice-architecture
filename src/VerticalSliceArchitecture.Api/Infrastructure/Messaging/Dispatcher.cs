using System.Collections.Concurrent;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Infrastructure.Messaging;

/// <summary>
/// Resolves the handler for a request and invokes it.
/// <para>
/// The awkward part of any mediator is that the concrete request type is only known at runtime
/// while the handler interface is generic. Rather than reflecting on every call, each request type
/// is mapped once to a small generic wrapper and the wrapper is cached — so the reflection cost is
/// paid on first use and never again. This is the same trick MediatR uses, in about sixty lines.
/// </para>
/// </summary>
public sealed class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    // Keyed by concrete request type. A request implements exactly one of ICommand, ICommand<T>
    // or IQuery<T>, so one cache serves all three without collisions.
    private static readonly ConcurrentDictionary<Type, object> Wrappers = new();

    public Task<Result> SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var wrapper = (CommandWrapperBase)Wrappers.GetOrAdd(
            command.GetType(),
            static type => Activate(typeof(CommandWrapper<>).MakeGenericType(type)));

        return wrapper.HandleAsync(command, serviceProvider, cancellationToken);
    }

    public Task<Result<TResponse>> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var wrapper = (ResultCommandWrapperBase<TResponse>)Wrappers.GetOrAdd(
            command.GetType(),
            static type => Activate(typeof(ResultCommandWrapper<,>).MakeGenericType(type, typeof(TResponse))));

        return wrapper.HandleAsync(command, serviceProvider, cancellationToken);
    }

    public Task<Result<TResponse>> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var wrapper = (QueryWrapperBase<TResponse>)Wrappers.GetOrAdd(
            query.GetType(),
            static type => Activate(typeof(QueryWrapper<,>).MakeGenericType(type, typeof(TResponse))));

        return wrapper.HandleAsync(query, serviceProvider, cancellationToken);
    }

    private static object Activate(Type wrapperType) =>
        Activator.CreateInstance(wrapperType)
        ?? throw new InvalidOperationException($"Could not create dispatcher wrapper '{wrapperType}'.");

    private abstract class CommandWrapperBase
    {
        public abstract Task<Result> HandleAsync(
            ICommand command,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken);
    }

    private sealed class CommandWrapper<TCommand> : CommandWrapperBase
        where TCommand : ICommand
    {
        public override Task<Result> HandleAsync(
            ICommand command,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken) =>
            serviceProvider
                .GetRequiredService<ICommandHandler<TCommand>>()
                .HandleAsync((TCommand)command, cancellationToken);
    }

    private abstract class ResultCommandWrapperBase<TResponse>
    {
        public abstract Task<Result<TResponse>> HandleAsync(
            ICommand<TResponse> command,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken);
    }

    private sealed class ResultCommandWrapper<TCommand, TResponse> : ResultCommandWrapperBase<TResponse>
        where TCommand : ICommand<TResponse>
    {
        public override Task<Result<TResponse>> HandleAsync(
            ICommand<TResponse> command,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken) =>
            serviceProvider
                .GetRequiredService<ICommandHandler<TCommand, TResponse>>()
                .HandleAsync((TCommand)command, cancellationToken);
    }

    private abstract class QueryWrapperBase<TResponse>
    {
        public abstract Task<Result<TResponse>> HandleAsync(
            IQuery<TResponse> query,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken);
    }

    private sealed class QueryWrapper<TQuery, TResponse> : QueryWrapperBase<TResponse>
        where TQuery : IQuery<TResponse>
    {
        public override Task<Result<TResponse>> HandleAsync(
            IQuery<TResponse> query,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken) =>
            serviceProvider
                .GetRequiredService<IQueryHandler<TQuery, TResponse>>()
                .HandleAsync((TQuery)query, cancellationToken);
    }
}
