using VerticalSliceArchitecture.Api.Domain.Common;
using VerticalSliceArchitecture.Api.Domain.Common.Enums;
using VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;
using VerticalSliceArchitecture.Api.Domain.Common.ValueObjects;

namespace VerticalSliceArchitecture.Api.Domain.Entities;

public sealed class Order : AggregateRoot<OrderId>
{
	private readonly List<OrderItem> _items = [];

	public Guid CustomerId { get; private set; }
	public OrderStatus Status { get; private set; }
	public DateTime CreatedAtUtc { get; private set; }

	// Read-only access to encapsulate internal collection state
	public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

	public Money TotalAmount => _items.Count == 0
		? Money.Zero()
		: _items.Aggregate(Money.Zero(_items[0].UnitPrice.Currency), (acc, item) => acc + item.TotalPrice);

	private Order(OrderId id, Guid customerId) : base(id)
	{
		CustomerId = customerId;
		Status = OrderStatus.Pending;
		CreatedAtUtc = DateTime.UtcNow;
	}

	// Factory method enforcing proper creation state
	public static Order Create(Guid customerId)
	{
		if (customerId == Guid.Empty)
			throw new ArgumentException("Customer ID cannot be empty.", nameof(customerId));

		var order = new Order(OrderId.New(), customerId);

		order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, order.CustomerId));

		return order;
	}

	// Business Operation: Add item or update quantity if already present
	public void AddItem(ProductId productId, int quantity, Money unitPrice)
	{
		EnsureOrderIsPending();

		if (quantity <= 0)
			throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

		var existingItem = _items.FirstOrDefault(x => x.ProductId == productId);

		if (existingItem is not null)
		{
			existingItem.UpdateQuantity(quantity);
		}
		else
		{
			var newItem = new OrderItem(OrderItemId.New(), productId, quantity, unitPrice);
			_items.Add(newItem);
		}
	}

	// Business Operation: Transition state to Paid
	public void MarkAsPaid()
	{
		EnsureOrderIsPending();

		if (_items.Count == 0)
			throw new InvalidOperationException("Cannot complete payment on an empty order.");

		Status = OrderStatus.Paid;

		RaiseDomainEvent(new OrderPaidEvent(Id, TotalAmount.Amount));
	}

	// Business Operation: Cancel order
	public void Cancel()
	{
		if (Status == OrderStatus.Shipped)
			throw new InvalidOperationException("Cannot cancel an order that has already been shipped.");

		if (Status == OrderStatus.Cancelled)
			return;

		Status = OrderStatus.Cancelled;

		RaiseDomainEvent(new OrderCancelledEvent(Id));
	}

	private void EnsureOrderIsPending()
	{
		if (Status != OrderStatus.Pending)
			throw new InvalidOperationException($"Cannot modify order in state '{Status}'.");
	}

#pragma warning disable CS8618
	private Order() { } // EF Core constructor
#pragma warning restore CS8618
}

// Order Domain Events
public record OrderCreatedEvent(OrderId OrderId, Guid CustomerId) : DomainEvent;
public record OrderPaidEvent(OrderId OrderId, decimal TotalPaidAmount) : DomainEvent;
public record OrderCancelledEvent(OrderId OrderId) : DomainEvent;