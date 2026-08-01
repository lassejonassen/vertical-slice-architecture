using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;
using VerticalSliceArchitecture.Api.Domain.Entities;

namespace VerticalSliceArchitecture.Api.Common.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<Product> Products => Set<Product>();
	public DbSet<Order> Orders => Set<Order>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

		base.OnModelCreating(modelBuilder);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		// Global conversion for ProductId across the entire DbContext
		configurationBuilder
			.Properties<ProductId>()
			.HaveConversion<ProductIdValueConverter>();

		configurationBuilder
			.Properties<OrderId>()
			.HaveConversion<OrderIdValueConverter>();

		configurationBuilder
			.Properties<OrderItemId>()
			.HaveConversion<OrderItemIdValueConverter>();

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

public sealed class OrderItemIdValueConverter : ValueConverter<OrderItemId, Guid>
{
	public OrderItemIdValueConverter()
		: base(id => id.Value, value => new OrderItemId(value)) { }
}