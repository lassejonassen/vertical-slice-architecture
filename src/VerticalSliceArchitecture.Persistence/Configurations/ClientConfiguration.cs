using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VerticalSliceArchitecture.Domain.Clients;

namespace VerticalSliceArchitecture.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(client => client.Id);

        // Value objects are single-valued here, so a conversion is lighter than an owned type.
        // Switch to OwnsOne when a value object has more than one component (e.g. Money).
        builder.Property(client => client.Name)
            .HasConversion(name => name.Value, value => CompanyName.Create(value).Value)
            .HasColumnName("name")
            .HasMaxLength(CompanyName.MaxLength)
            .IsRequired();

        builder.Property(client => client.ContactEmail)
            .HasConversion(email => email.Value, value => EmailAddress.Create(value).Value)
            .HasColumnName("contact_email")
            .HasMaxLength(EmailAddress.MaxLength)
            .IsRequired();

        builder.Property(client => client.Status)
            .HasConversion<int>()
            .HasColumnName("status")
            .IsRequired();

        builder.Property(client => client.RegisteredOnUtc).HasColumnName("registered_on_utc");
        builder.Property(client => client.DeactivatedOnUtc).HasColumnName("deactivated_on_utc");

        // The real uniqueness guarantee. The handler's pre-check gives a friendly 409;
        // this index is what actually holds under concurrent inserts.
        builder.HasIndex(client => client.ContactEmail)
            .IsUnique()
            .HasDatabaseName("ix_clients_contact_email");

        builder.HasIndex(client => client.Status).HasDatabaseName("ix_clients_status");

        // Domain events live in memory only; they are dispatched and discarded, never persisted.
        // Replace with an outbox table if you need at-least-once delivery.
        builder.Ignore(client => client.DomainEvents);

        builder.ConfigureAuditColumns();
    }
}
