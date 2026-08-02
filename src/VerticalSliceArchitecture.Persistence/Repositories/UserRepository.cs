using VerticalSliceArchitecture.Domain.Users;

namespace VerticalSliceArchitecture.Persistence.Repositories;

internal sealed class UserRepository(ApplicationDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByExternalIdentityAsync(
        ExternalIdentity identity,
        CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(
            user => user.Identity.Issuer == identity.Issuer && user.Identity.Subject == identity.Subject,
            cancellationToken);

    public void Add(User user) => context.Users.Add(user);
}
