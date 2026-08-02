using VerticalSliceArchitecture.Api.Infrastructure.Endpoints;
using VerticalSliceArchitecture.Api.Infrastructure.Messaging;
using VerticalSliceArchitecture.Api.Infrastructure.RateLimiting;
using VerticalSliceArchitecture.Api.Infrastructure.Security;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Features.Clients.GetClientById;

/// <summary>
/// Demonstrates the <b>dispatcher</b> style, for contrast with <c>RegisterClientEndpoint</c>.
/// Both styles are wired up and both are correct; pick per slice rather than per solution.
/// </summary>
internal sealed class GetClientByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/clients/{id:guid}", async (
                Guid id,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
        {
            Result<ClientDetailsResponse> result = await dispatcher.QueryAsync(
                new GetClientByIdQuery(id),
                cancellationToken);

            return result.ToOk();
        })
            .WithName("GetClientById")
            .WithSummary("Returns a single client by identifier.")
            .WithTags("Clients")
            .RequireAuthorization(AuthorizationPolicies.ReadClients)
            .RequireRateLimiting(RateLimitPolicies.PerUser)
            .Produces<ClientDetailsResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
}
