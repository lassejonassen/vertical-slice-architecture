using VerticalSliceArchitecture.Api.Domain.Common;
using VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;

namespace VerticalSliceArchitecture.Api.Domain.Entities;


public sealed class Product : AggregateRoot<ProductId>
{
	public string Name { get; private set; } = string.Empty;
	public decimal Price { get; private set; }

	private Product(ProductId id, string name, decimal price) : base(id)
	{
		Name = name;
		Price = price;
	}

	// Factory method enforcing domain rules & raising events
	public static Product Create(string name, decimal price)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Product name cannot be empty.", nameof(name));

		if (price <= 0)
			throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive.");

		var product = new Product(ProductId.New(), name, price);

		product.RaiseDomainEvent(new ProductCreatedEvent(product.Id, product.Name, product.Price));

		return product;
	}

	public void UpdatePrice(decimal newPrice)
	{
		if (newPrice <= 0)
			throw new ArgumentOutOfRangeException(nameof(newPrice), "Price must be positive.");

		Price = newPrice;
	}

#pragma warning disable CS8618
	private Product() { } // Required for EF Core
#pragma warning restore CS8618
}

public record ProductCreatedEvent(ProductId Id, string Name, decimal Price) : DomainEvent;