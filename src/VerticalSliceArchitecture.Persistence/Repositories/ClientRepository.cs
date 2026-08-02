using VerticalSliceArchitecture.Domain.Clients;

namespace VerticalSliceArchitecture.Persistence.Repositories;

internal sealed class ClientRepository(ApplicationDbContext context) : IClientRepository
{
    public Task<Client?> GetByIdAsync(ClientId id, CancellationToken cancellationToken = default) =>
        context.Clients.FirstOrDefaultAsync(client => client.Id == id, cancellationToken);

    public Task<bool> ExistsWithEmailAsync(
        EmailAddress email,
        ClientId? excluding = null,
        CancellationToken cancellationToken = default) =>
        context.Clients
            .AsNoTracking()
            .Where(client => client.ContactEmail == email)
            .Where(client => excluding == null || client.Id != excluding.Value)
            .AnyAsync(cancellationToken);

    // No SaveChanges here. Committing is the handler's decision, made through IUnitOfWork, so that
    // several aggregate changes can share one transaction.
    public void Add(Client client) => context.Clients.Add(client);
}
