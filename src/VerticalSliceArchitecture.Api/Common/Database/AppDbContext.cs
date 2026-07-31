using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;
using VerticalSliceArchitecture.Api.Domain.Entities;

namespace VerticalSliceArchitecture.Api.Common.Database;

public class AppDbContext : DbContext
{
	public DbSet<Product> Products => Set<Product>();
	public DbSet<Order> Orders => Set<Order>();

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		// Global conversion for ProductId across the entire DbContext
		configurationBuilder
			.Properties<ProductId>()
			.HaveConversion<ProductIdValueConverter>();

		configurationBuilder
			.Properties<OrderId>()
			.HaveConversion<OrderIdValueConverter>();

		base.ConfigureConventions(configurationBuilder);
	}
}

public sealed class ProductIdValueConverter : ValueConverter<ProductId, Guid>
{
	public ProductIdValueConverter()
		: base(id => id.Value, value => new ProductId(value)) { }
}

public sealed class OrderIdValueConverter : ValueConverter<OrderId, Guid>
{
	public OrderIdValueConverter()
		: base(id => id.Value, value => new OrderId(value)) { }
}