using VerticalSliceArchitecture.Api.Infrastructure.Endpoints;
using VerticalSliceArchitecture.Api.Infrastructure.Endpoints.Filters;
using VerticalSliceArchitecture.Api.Infrastructure.Messaging;
using VerticalSliceArchitecture.Api.Infrastructure.RateLimiting;
using VerticalSliceArchitecture.Api.Infrastructure.Security;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Features.Clients.RegisterClient;

/// <summary>
/// Demonstrates the <b>service-based</b> style: the handler is injected straight into the route
/// delegate. No dispatcher, no reflection, and "go to implementation" lands on the real code.
/// This is the default choice for a slice with a single caller.
/// </summary>
internal sealed class RegisterClientEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/clients", async (
                RegisterClientRequest request,
                ICommandHandler<RegisterClientCommand, RegisterClientResponse> handler,
                CancellationToken cancellationToken) =>
        {
            Result<RegisterClientResponse> result = await handler.HandleAsync(
                new RegisterClientCommand(request.CompanyName, request.ContactEmail),
                cancellationToken);

            return result.ToCreated(response => $"/clients/{response.Id}");
        })
            .WithName("RegisterClient")
            .WithSummary("Registers a new client.")
            .WithTags("Clients")
            .WithValidation<RegisterClientRequest>()
            .RequireAuthorization(AuthorizationPolicies.ManageClients)
            .RequireRateLimiting(RateLimitPolicies.Sensitive)
            .Produces<RegisterClientResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);
}
