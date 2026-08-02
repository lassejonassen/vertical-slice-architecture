namespace VerticalSliceArchitecture.Domain.Clients;

/// <summary>
/// Write-side access to the <see cref="Client"/> aggregate.
/// <para>
/// Intentionally narrow. Query slices do not use this — they project straight to DTOs against the
/// read model, because loading a full aggregate to render a list is wasted work and couples the
/// read shape to the write shape.
/// </para>
/// </summary>
public interface IClientRepository
{
    public Task<Client?> GetByIdAsync(ClientId id, CancellationToken cancellationToken = default);

    public Task<bool> ExistsWithEmailAsync(
        EmailAddress email,
        ClientId? excluding = null,
        CancellationToken cancellationToken = default);

    public void Add(Client client);
}
