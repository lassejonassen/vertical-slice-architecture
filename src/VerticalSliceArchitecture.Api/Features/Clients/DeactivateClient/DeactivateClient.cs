using VerticalSliceArchitecture.Api.Infrastructure.Endpoints;
using VerticalSliceArchitecture.Api.Infrastructure.Messaging;
using VerticalSliceArchitecture.Api.Infrastructure.RateLimiting;
using VerticalSliceArchitecture.Api.Infrastructure.Security;
using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.SharedKernel.Abstractions;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Features.Clients.DeactivateClient;

public sealed record DeactivateClientCommand(Guid Id) : ICommand;

/// <summary>
/// A write that <em>does</em> load the aggregate, unlike the query slices — because it changes
/// state, and the rule about not deactivating twice belongs to the aggregate.
/// </summary>
internal sealed class DeactivateClientHandler(
    IClientRepository clients,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeactivateClientCommand>
{
    public async Task<Result> HandleAsync(
        DeactivateClientCommand command,
        CancellationToken cancellationToken = default)
    {
        Client? client = await clients.GetByIdAsync(ClientId.From(command.Id), cancellationToken);

        if (client is null)
        {
            return ClientErrors.NotFound;
        }

        Result deactivated = client.Deactivate(dateTimeProvider.UtcNow);

        if (deactivated.IsFailure)
        {
            return deactivated;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

internal sealed class DeactivateClientEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/clients/{id:guid}/deactivate", async (
                Guid id,
                ICommandHandler<DeactivateClientCommand> handler,
                CancellationToken cancellationToken) =>
        {
            Result result = await handler.HandleAsync(new DeactivateClientCommand(id), cancellationToken);

            return result.ToNoContent();
        })
            .WithName("DeactivateClient")
            .WithSummary("Deactivates a client.")
            .WithTags("Clients")
            .RequireAuthorization(AuthorizationPolicies.ManageClients)
            .RequireRateLimiting(RateLimitPolicies.Sensitive)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
}
