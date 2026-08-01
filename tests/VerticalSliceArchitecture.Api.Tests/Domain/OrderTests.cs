using VerticalSliceArchitecture.Api.Domain.Common.Enums;
using VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;
using VerticalSliceArchitecture.Api.Domain.Common.ValueObjects;
using VerticalSliceArchitecture.Api.Domain.Entities;

namespace VerticalSliceArchitecture.Api.Tests.Domain;

public class OrderTests
{
	private static readonly Guid CustomerId = Guid.NewGuid();

	[Fact]
	public void Create_WithValidCustomerId_SetsPendingStatusAndRaisesEvent()
	{
		var order = Order.Create(CustomerId);

		Assert.Equal(CustomerId, order.CustomerId);
		Assert.Equal(OrderStatus.Pending, order.Status);
		var raised = Assert.Single(order.DomainEvents);
		Assert.IsType<OrderCreatedEvent>(raised);
	}

	[Fact]
	public void Create_WithEmptyCustomerId_Throws()
	{
		var act = () => Order.Create(Guid.Empty);

		Assert.Throws<ArgumentException>(act);
	}

	[Fact]
	public void AddItem_NewProduct_AddsItem()
	{
		var order = Order.Create(CustomerId);
		var productId = ProductId.New();

		order.AddItem(productId, 2, Money.Create(10m));

		var item = Assert.Single(order.Items);
		Assert.Equal(productId, item.ProductId);
		Assert.Equal(2, item.Quantity);
	}

	[Fact]
	public void AddItem_ExistingProduct_MergesQuantityInsteadOfDuplicating()
	{
		var order = Order.Create(CustomerId);
		var productId = ProductId.New();

		order.AddItem(productId, 2, Money.Create(10m));
		order.AddItem(productId, 3, Money.Create(10m));

		var item = Assert.Single(order.Items);
		Assert.Equal(5, item.Quantity);
	}

	[Fact]
	public void AddItem_NonPositiveQuantity_Throws()
	{
		var order = Order.Create(CustomerId);

		var act = () => order.AddItem(ProductId.New(), 0, Money.Create(10m));

		Assert.Throws<ArgumentOutOfRangeException>(act);
	}

	[Fact]
	public void AddItem_WhenOrderIsCancelled_Throws()
	{
		var order = Order.Create(CustomerId);
		order.Cancel();

		var act = () => order.AddItem(ProductId.New(), 1, Money.Create(10m));

		Assert.Throws<InvalidOperationException>(act);
	}

	[Fact]
	public void TotalAmount_WithNoItems_IsZero()
	{
		var order = Order.Create(CustomerId);

		Assert.Equal(0m, order.TotalAmount.Amount);
	}

	[Fact]
	public void TotalAmount_WithMultipleItems_SumsLineTotals()
	{
		var order = Order.Create(CustomerId);
		order.AddItem(ProductId.New(), 2, Money.Create(10m));
		order.AddItem(ProductId.New(), 1, Money.Create(5m));

		Assert.Equal(25m, order.TotalAmount.Amount);
	}

	[Fact]
	public void MarkAsPaid_WithItems_SetsStatusPaidAndRaisesEvent()
	{
		var order = Order.Create(CustomerId);
		order.AddItem(ProductId.New(), 1, Money.Create(10m));

		order.MarkAsPaid();

		Assert.Equal(OrderStatus.Paid, order.Status);
		Assert.Contains(order.DomainEvents, e => e is OrderPaidEvent);
	}

	[Fact]
	public void MarkAsPaid_WithoutItems_Throws()
	{
		var order = Order.Create(CustomerId);

		var act = order.MarkAsPaid;

		Assert.Throws<InvalidOperationException>(act);
	}

	[Fact]
	public void MarkAsPaid_WhenAlreadyPaid_Throws()
	{
		var order = Order.Create(CustomerId);
		order.AddItem(ProductId.New(), 1, Money.Create(10m));
		order.MarkAsPaid();

		var act = order.MarkAsPaid;

		Assert.Throws<InvalidOperationException>(act);
	}

	[Fact]
	public void Cancel_WhenPending_SetsCancelledAndRaisesEvent()
	{
		var order = Order.Create(CustomerId);

		order.Cancel();

		Assert.Equal(OrderStatus.Cancelled, order.Status);
		Assert.Contains(order.DomainEvents, e => e is OrderCancelledEvent);
	}

	[Fact]
	public void Cancel_WhenAlreadyCancelled_IsIdempotentAndRaisesNoAdditionalEvent()
	{
		var order = Order.Create(CustomerId);
		order.Cancel();
		order.ClearDomainEvents();

		order.Cancel();

		Assert.Equal(OrderStatus.Cancelled, order.Status);
		Assert.Empty(order.DomainEvents);
	}

	[Fact]
	public void Cancel_WhenShipped_Throws()
	{
		var order = Order.Create(CustomerId);
		order.AddItem(ProductId.New(), 1, Money.Create(10m));
		order.MarkAsPaid();
		SetStatusToShipped(order);

		var act = order.Cancel;

		Assert.Throws<InvalidOperationException>(act);
	}

	// Order has no "ship" behavior yet (no feature exposes that transition), so the
	// only way to exercise the Cancel-when-Shipped guard is to force the state directly.
	private static void SetStatusToShipped(Order order)
	{
		typeof(Order).GetProperty(nameof(Order.Status))!.SetValue(order, OrderStatus.Shipped);
	}
}
