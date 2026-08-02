using Microsoft.EntityFrameworkCore.Metadata;

namespace VerticalSliceArchitecture.Persistence.Configurations;

/// <summary>
/// Adds an optimistic concurrency token to every aggregate root, in whatever form the active
/// provider supports. Applied once in <c>OnModelCreating</c> rather than repeated in each
/// entity configuration, so adding an aggregate cannot accidentally omit it.
/// </summary>
internal static class ConcurrencyTokenConventions
{
    public const string PostgreSqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";
    public const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

    public static ModelBuilder ApplyConcurrencyTokens(this ModelBuilder modelBuilder, string? providerName)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!IsAggregateRoot(entityType.ClrType))
            {
                continue;
            }

            switch (providerName)
            {
                case PostgreSqlProvider:
                    // xmin is a system column PostgreSQL maintains itself; no schema change required.
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<uint>("xmin")
                        .HasColumnName("xmin")
                        .IsRowVersion();
                    break;

                case SqlServerProvider:
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<byte[]>("RowVersion")
                        .HasColumnName("row_version")
                        .IsRowVersion();
                    break;

                default:
                    // In-memory or SQLite (used by some tests): no token, and that is fine because
                    // those providers are not run concurrently.
                    break;
            }
        }

        return modelBuilder;
    }

    private static bool IsAggregateRoot(Type type)
    {
        for (Type? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }
        }

        return false;
    }
}
