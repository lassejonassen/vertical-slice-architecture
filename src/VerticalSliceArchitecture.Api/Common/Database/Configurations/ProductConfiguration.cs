using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VerticalSliceArchitecture.Api.Domain.Entities;

namespace VerticalSliceArchitecture.Api.Common.Database.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
	public void Configure(EntityTypeBuilder<Product> builder)
	{
		builder.ToTable("Products");

		// Set primary key
		builder.HasKey(p => p.Id);

		// Property Constraints
		builder.Property(p => p.Name)
			.HasMaxLength(200)
			.IsRequired();

		builder.Property(p => p.Price)
			.HasPrecision(18, 2)
			.IsRequired();

		// Ignore Domain Events so EF Core doesn't attempt to persist them
		builder.Ignore(p => p.DomainEvents);
	}
}