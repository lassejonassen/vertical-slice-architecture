using VerticalSliceArchitecture.Api.Domain.Common;
using VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;
using VerticalSliceArchitecture.Api.Domain.Common.ValueObjects;

namespace VerticalSliceArchitecture.Api.Domain.Entities;

public sealed class OrderItem : Entity<OrderItemId>
{
	public ProductId ProductId { get; private set; }
	public int Quantity { get; private set; }
	public Money UnitPrice { get; private set; }

	public Money TotalPrice => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);

	internal OrderItem(OrderItemId id, ProductId productId, int quantity, Money unitPrice)
		: base(id)
	{
		ProductId = productId;
		Quantity = quantity;
		UnitPrice = unitPrice;
	}

	internal void UpdateQuantity(int additionalQuantity)
	{
		if (additionalQuantity <= 0)
			throw new ArgumentOutOfRangeException(nameof(additionalQuantity), "Quantity step must be greater than zero.");

		Quantity += additionalQuantity;
	}

#pragma warning disable CS8618
	private OrderItem() { } // EF Core constructor
#pragma warning restore CS8618
}