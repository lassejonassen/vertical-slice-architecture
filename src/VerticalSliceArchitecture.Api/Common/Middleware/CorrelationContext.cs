namespace VerticalSliceArchitecture.Api.Common.Middleware;

public interface ICorrelationContext
{
	Guid CorrelationId { get; }
}

public interface ICorrelationIdSetter
{
	void Set(Guid correlationId);
}

// Scoped per HTTP request: CorrelationIdMiddleware resolves this once, optionally
// overwrites it from the incoming header via the setter interface, and everything
// else in the request only ever sees the read-only ICorrelationContext.
public sealed class CorrelationContext : ICorrelationContext, ICorrelationIdSetter
{
	public Guid CorrelationId { get; private set; } = Guid.NewGuid();

	public void Set(Guid correlationId) => CorrelationId = correlationId;
}
