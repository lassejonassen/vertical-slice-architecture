namespace VerticalSliceArchitecture.Domain.Users;

public interface IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);

    public Task<User?> GetByExternalIdentityAsync(
        ExternalIdentity identity,
        CancellationToken cancellationToken = default);

    public void Add(User user);
}
