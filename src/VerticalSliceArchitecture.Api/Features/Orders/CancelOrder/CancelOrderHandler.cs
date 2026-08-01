using Microsoft.EntityFrameworkCore;
using VerticalSliceArchitecture.Api.Common.Database;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;
using VerticalSliceArchitecture.Api.Domain.Common.Enums;
using VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;

namespace VerticalSliceArchitecture.Api.Features.Orders.CancelOrder;

public sealed class CancelOrderHandler(AppDbContext dbContext) : IRequestHandler<CancelOrderCommand, Result>
{
	public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
	{
		var orderId = new OrderId(request.OrderId);

		var order = await dbContext.Orders
			.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

		if (order is null)
		{
			return Result.Failure(new Error(
				"Order.NotFound",
				$"Order with Id '{request.OrderId}' was not found.",
				ErrorType.NotFound));
		}

		if (order.Status == OrderStatus.Shipped)
		{
			return Result.Failure(new Error(
				"Order.AlreadyShipped",
				"Cannot cancel an order that has already been shipped.",
				ErrorType.Conflict));
		}

		order.Cancel();

		await dbContext.SaveChangesAsync(cancellationToken);

		return Result.Success();
	}
}
