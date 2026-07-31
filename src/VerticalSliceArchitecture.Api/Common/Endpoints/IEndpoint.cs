namespace VerticalSliceArchitecture.Api.Common.Endpoints;

/// <summary>
/// Defines a contract for Minimal API endpoints to self-register their routes.
/// </summary>
public interface IEndpoint
{
	void MapEndpoint(IEndpointRouteBuilder app);
}