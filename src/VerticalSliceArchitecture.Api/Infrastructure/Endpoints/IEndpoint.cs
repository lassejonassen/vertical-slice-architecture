namespace VerticalSliceArchitecture.Api.Infrastructure.Endpoints;

/// <summary>
/// One endpoint, declared next to the handler it calls.
/// <para>
/// The point of this interface is that adding a feature never means editing a shared file. A slice
/// is a folder you add; nothing outside it changes. That property is what makes vertical slices
/// worth the trouble, and a central <c>MapEndpoints</c> switchboard would give it away immediately.
/// </para>
/// </summary>
public interface IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app);
}

