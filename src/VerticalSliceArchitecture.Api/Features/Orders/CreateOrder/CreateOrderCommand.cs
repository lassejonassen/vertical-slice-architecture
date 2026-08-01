using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Features.Orders.CreateOrder;

public sealed record CreateOrderCommand(Guid CustomerId, IReadOnlyCollection<CreateOrderCommandItem> Items)
	: IRequest<Result<CreateOrderResponse>>;

public sealed record CreateOrderCommandItem(Guid ProductId, int Quantity);
