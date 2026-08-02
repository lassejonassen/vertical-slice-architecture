using Microsoft.EntityFrameworkCore;
using VerticalSliceArchitecture.Api.Infrastructure.Messaging;
using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.Persistence;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Features.Clients.GetClientById;

public sealed record ClientDetailsResponse(
    Guid Id,
    string CompanyName,
    string ContactEmail,
    string Status,
    DateTimeOffset RegisteredOnUtc,
    DateTimeOffset? DeactivatedOnUtc);

public sealed record GetClientByIdQuery(Guid Id) : IQuery<ClientDetailsResponse>;

/// <summary>
/// Reads project straight to the response type instead of loading the aggregate.
/// <para>
/// This is where vertical slices and DDD stop pulling against each other. Writes must go through
/// <see cref="Client"/> so its invariants hold; reads have no invariants to protect, so hydrating
/// an aggregate, its value objects and its event list only to copy six fields out is pure
/// overhead — and it couples the response shape to the write model, so every read breaks when the
/// aggregate is refactored.
/// </para>
/// </summary>
internal sealed class GetClientByIdHandler(ApplicationDbContext context)
    : IQueryHandler<GetClientByIdQuery, ClientDetailsResponse>
{
    public async Task<Result<ClientDetailsResponse>> HandleAsync(
        GetClientByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ClientDetailsResponse? client = await context.ClientsView
            .AsNoTracking()
            .Where(entity => entity.Id == query.Id)
            .Select(entity => new ClientDetailsResponse(
                entity.Id,
                entity.Name,
                entity.ContactEmail,
                ((ClientStatus)entity.Status).ToString(),
                entity.RegisteredOnUtc,
                entity.DeactivatedOnUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return client is null
            ? Result.Failure<ClientDetailsResponse>(ClientErrors.NotFound)
            : Result.Success(client);
    }
}
