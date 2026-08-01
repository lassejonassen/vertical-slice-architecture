namespace VerticalSliceArchitecture.Api.Features.Orders.CreateOrder;

public sealed record CreateOrderResponse(
	Guid Id,
	Guid CustomerId,
	string Status,
	IReadOnlyCollection<CreateOrderResponseItem> Items,
	decimal TotalAmount,
	string Currency);

public sealed record CreateOrderResponseItem(Guid ProductId, int Quantity, decimal UnitPrice, decimal TotalPrice);
