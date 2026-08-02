using Microsoft.EntityFrameworkCore;
using VerticalSliceArchitecture.Api.Infrastructure.Endpoints;
using VerticalSliceArchitecture.Api.Infrastructure.Messaging;
using VerticalSliceArchitecture.Api.Infrastructure.RateLimiting;
using VerticalSliceArchitecture.Api.Infrastructure.Security;
using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.Persistence;
using VerticalSliceArchitecture.Persistence.ReadModels;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Features.Clients.SearchClients;

public sealed record ClientSummary(Guid Id, string CompanyName, string ContactEmail, string Status);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;
}

public sealed record SearchClientsQuery(
    string? SearchTerm,
    bool? ActiveOnly,
    int Page,
    int PageSize) : IQuery<PagedResult<ClientSummary>>;

internal sealed class SearchClientsHandler(ApplicationDbContext context)
    : IQueryHandler<SearchClientsQuery, PagedResult<ClientSummary>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<ClientSummary>>> HandleAsync(
        SearchClientsQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = Math.Max(query.Page, 1);
        // Clamp rather than reject: a client asking for 10,000 rows gets 100, not a 400 it has to
        // handle. The cap is what protects the database, and it belongs server-side regardless.
        int pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        // ClientsView rather than Clients. EF cannot translate `client.Name.Value` on a
        // value-converted property, so anything needing LIKE or ORDER BY on the inner string
        // goes through the read model. See ClientReadModel for the full explanation.
        IQueryable<ClientReadModel> clients = context.ClientsView.AsNoTracking();

        if (query.ActiveOnly == true)
        {
            clients = clients.Where(client => client.Status == (int)ClientStatus.Active);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = $"%{query.SearchTerm.Trim()}%";

            // EF.Functions.Like translates to SQL LIKE on both providers. A leading wildcard cannot
            // use a B-tree index, so swap this for full-text search once the table is large.
            clients = clients.Where(client =>
                EF.Functions.Like(client.Name, term)
                || EF.Functions.Like(client.ContactEmail, term));
        }

        int totalCount = await clients.CountAsync(cancellationToken);

        List<ClientSummary> items = await clients
            // Ordering by the key as a tiebreaker keeps paging stable when names collide;
            // without it, rows can appear on two pages or on none.
            .OrderBy(client => client.Name)
            .ThenBy(client => client.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(client => new ClientSummary(
                client.Id,
                client.Name,
                client.ContactEmail,
                ((ClientStatus)client.Status).ToString()))
            .ToListAsync(cancellationToken);

        return new PagedResult<ClientSummary>(items, page, pageSize, totalCount);
    }
}

internal sealed class SearchClientsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/clients", async (
                IDispatcher dispatcher,
                CancellationToken cancellationToken,
                string? search = null,
                bool? activeOnly = null,
                int page = 1,
                int pageSize = 25) =>
        {
            Result<PagedResult<ClientSummary>> result = await dispatcher.QueryAsync(
                new SearchClientsQuery(search, activeOnly, page, pageSize),
                cancellationToken);

            return result.ToOk();
        })
            .WithName("SearchClients")
            .WithSummary("Returns a paged list of clients.")
            .WithTags("Clients")
            .RequireAuthorization(AuthorizationPolicies.ReadClients)
            .RequireRateLimiting(RateLimitPolicies.Burst)
            .Produces<PagedResult<ClientSummary>>();
}
