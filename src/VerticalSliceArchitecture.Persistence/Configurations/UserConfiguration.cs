using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VerticalSliceArchitecture.Domain.Users;

namespace VerticalSliceArchitecture.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        // ExternalIdentity has two components, so it maps as an owned type rather than a conversion.
        builder.OwnsOne(user => user.Identity, identity =>
        {
            identity.Property(value => value.Issuer)
                .HasColumnName("identity_issuer")
                .HasMaxLength(500)
                .IsRequired();

            identity.Property(value => value.Subject)
                .HasColumnName("identity_subject")
                .HasMaxLength(500)
                .IsRequired();

            // The lookup on every authenticated request goes through this index.
            identity.HasIndex(value => new { value.Issuer, value.Subject })
                .IsUnique()
                .HasDatabaseName("ix_users_external_identity");
        });

        builder.Navigation(user => user.Identity).IsRequired();

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(user => user.LastSeenOnUtc).HasColumnName("last_seen_on_utc");

        builder.Ignore(user => user.DomainEvents);

        builder.ConfigureAuditColumns();
    }
}
