using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.Domain.Users;
using VerticalSliceArchitecture.Persistence.Configurations;
using VerticalSliceArchitecture.Persistence.Converters;
using VerticalSliceArchitecture.Persistence.ReadModels;

namespace VerticalSliceArchitecture.Persistence;

/// <summary>
/// The write model. Feature handlers depend on <see cref="IUnitOfWork"/> and the repository
/// interfaces rather than on this type; query slices may inject it directly and project with
/// <c>AsNoTracking</c>, because a read that never mutates does not need the aggregate.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public const string DefaultSchema = "app";

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<User> Users => Set<User>();

    /// <summary>Read-only projection used by query slices. See <see cref="ClientReadModel"/>.</summary>
    public DbSet<ClientReadModel> ClientsView => Set<ClientReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.ApplyConcurrencyTokens(Database.ProviderName);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Register every strongly-typed ID once, so individual entity configurations do not each
        // have to remember a HasConversion call.
        configurationBuilder.RegisterStronglyTypedIds();

        configurationBuilder.Properties<string>().HaveMaxLength(500);

        base.ConfigureConventions(configurationBuilder);
    }
}
