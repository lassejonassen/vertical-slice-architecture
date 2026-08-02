using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VerticalSliceArchitecture.Persistence.Configurations;

/// <summary>
/// Shared mapping for the audit columns, applied by every aggregate configuration so the
/// conventions cannot drift apart. Concurrency tokens are handled separately and per provider —
/// see <c>ConcurrencyTokenConventions</c>.
/// </summary>
internal static class AuditConfigurationExtensions
{
    public static EntityTypeBuilder<TEntity> ConfigureAuditColumns<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable
    {
        builder.Property(entity => entity.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        builder.Property(entity => entity.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
        builder.Property(entity => entity.ModifiedOnUtc).HasColumnName("modified_on_utc");
        builder.Property(entity => entity.ModifiedBy).HasColumnName("modified_by").HasMaxLength(256);

        return builder;
    }
}
