using VerticalSliceArchitecture.Api.Domain.Entities;

namespace VerticalSliceArchitecture.Api.Tests.Domain;

public class ProductTests
{
	[Fact]
	public void Create_WithValidData_SetsPropertiesAndRaisesEvent()
	{
		var product = Product.Create("Widget", 9.99m);

		Assert.Equal("Widget", product.Name);
		Assert.Equal(9.99m, product.Price);
		var raised = Assert.Single(product.DomainEvents);
		var createdEvent = Assert.IsType<ProductCreatedEvent>(raised);
		Assert.Equal(product.Id, createdEvent.Id);
		Assert.Equal(product.Name, createdEvent.Name);
		Assert.Equal(product.Price, createdEvent.Price);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Create_WithEmptyName_Throws(string name)
	{
		var act = () => Product.Create(name, 9.99m);

		Assert.Throws<ArgumentException>(act);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Create_WithNonPositivePrice_Throws(decimal price)
	{
		var act = () => Product.Create("Widget", price);

		Assert.Throws<ArgumentOutOfRangeException>(act);
	}

	[Fact]
	public void UpdatePrice_WithValidPrice_UpdatesPrice()
	{
		var product = Product.Create("Widget", 9.99m);

		product.UpdatePrice(12.5m);

		Assert.Equal(12.5m, product.Price);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void UpdatePrice_WithNonPositivePrice_Throws(decimal price)
	{
		var product = Product.Create("Widget", 9.99m);

		var act = () => product.UpdatePrice(price);

		Assert.Throws<ArgumentOutOfRangeException>(act);
	}

	[Fact]
	public void ClearDomainEvents_RemovesAllRaisedEvents()
	{
		var product = Product.Create("Widget", 9.99m);

		product.ClearDomainEvents();

		Assert.Empty(product.DomainEvents);
	}
}
