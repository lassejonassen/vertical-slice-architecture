using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Infrastructure.Messaging;

/// <summary>
/// Optional indirection between an endpoint and its handler.
/// <para>
/// Both styles are supported on purpose, and the choice is per slice rather than per solution:
/// </para>
/// <list type="bullet">
///   <item>
///     <b>Inject the handler directly</b> (<c>ICommandHandler&lt;RegisterClientCommand, Guid&gt;</c>).
///     Fewer moving parts, the call is statically resolved, and navigating to the implementation is
///     one keystroke. Right for most slices.
///   </item>
///   <item>
///     <b>Go through the dispatcher.</b> Earns its keep when a handler is invoked from several
///     places, when the request type is only known at runtime, or when you want one seam to hang
///     cross-cutting behaviour on. Costs a dictionary lookup and a virtual call.
///   </item>
/// </list>
/// <para>
/// Notably, this does <em>not</em> carry a behaviour pipeline. In a Minimal API, validation,
/// authorisation and idempotency compose better as endpoint filters, where they can see the HTTP
/// context and short-circuit before a handler is even constructed. Keeping the dispatcher dumb
/// avoids two competing places to put cross-cutting concerns.
/// </para>
/// </summary>
public interface IDispatcher
{
    public Task<Result> SendAsync(ICommand command, CancellationToken cancellationToken = default);

    public Task<Result<TResponse>> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default);

    public Task<Result<TResponse>> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default);
}
