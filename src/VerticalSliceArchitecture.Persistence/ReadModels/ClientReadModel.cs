using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VerticalSliceArchitecture.Persistence.ReadModels;

public sealed class ClientReadModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string ContactEmail { get; init; } = string.Empty;

    public int Status { get; init; }

    public DateTimeOffset RegisteredOnUtc { get; init; }

    public DateTimeOffset? DeactivatedOnUtc { get; init; }
}

internal sealed class ClientReadModelConfiguration : IEntityTypeConfiguration<ClientReadModel>
{
    public void Configure(EntityTypeBuilder<ClientReadModel> builder)
    {
        // ToView, not ToTable: same relation, read-only, excluded from migrations.
        builder.ToView("clients", ApplicationDbContext.DefaultSchema);

        builder.HasKey(client => client.Id);

        builder.Property(client => client.Id).HasColumnName("id");
        builder.Property(client => client.Name).HasColumnName("name");
        builder.Property(client => client.ContactEmail).HasColumnName("contact_email");
        builder.Property(client => client.Status).HasColumnName("status");
        builder.Property(client => client.RegisteredOnUtc).HasColumnName("registered_on_utc");
        builder.Property(client => client.DeactivatedOnUtc).HasColumnName("deactivated_on_utc");
    }
}
