using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;
using VerticalSliceArchitecture.Api.Domain.Entities;

namespace VerticalSliceArchitecture.Api.Common.Database.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
	public void Configure(EntityTypeBuilder<Order> builder)
	{
		builder.ToTable("Orders");

		builder.HasKey(o => o.Id);

		builder.Property(o => o.CustomerId)
			.IsRequired();

		builder.Property(o => o.Status)
			.HasConversion<int>()
			.IsRequired();

		// Owns Many for OrderItem child table
		builder.OwnsMany(o => o.Items, itemBuilder =>
		{
			itemBuilder.ToTable("OrderItems");

			itemBuilder.HasKey(i => i.Id);

			itemBuilder.Property(i => i.Id)
				.HasConversion(
					id => id.Value,
					value => new OrderItemId(value));

			itemBuilder.Property(i => i.ProductId)
				.HasConversion(
					id => id.Value,
					value => new ProductId(value));

			// Map Money Value Object owned properties
			itemBuilder.OwnsOne(i => i.UnitPrice, priceBuilder =>
			{
				priceBuilder.Property(p => p.Amount)
					.HasColumnName("UnitPriceAmount")
					.HasPrecision(18, 2)
					.IsRequired();

				priceBuilder.Property(p => p.Currency)
					.HasColumnName("Currency")
					.HasMaxLength(3)
					.IsRequired();
			});
		});

		// Ignore domain events during EF persistence
		builder.Ignore(o => o.DomainEvents);
	}
}