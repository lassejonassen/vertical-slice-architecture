using Microsoft.EntityFrameworkCore;
using VerticalSliceArchitecture.Api.Common.Database;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;
using VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;
using VerticalSliceArchitecture.Api.Domain.Common.ValueObjects;
using VerticalSliceArchitecture.Api.Domain.Entities;

namespace VerticalSliceArchitecture.Api.Features.Orders.CreateOrder;

public sealed class CreateOrderHandler(AppDbContext dbContext) : IRequestHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
	public async Task<Result<CreateOrderResponse>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
	{
		var productIds = request.Items
			.Select(item => new ProductId(item.ProductId))
			.Distinct()
			.ToList();

		var products = await dbContext.Products
			.AsNoTracking()
			.Where(p => productIds.Contains(p.Id))
			.ToDictionaryAsync(p => p.Id, cancellationToken);

		var missingProductIds = productIds.Except(products.Keys).ToList();

		if (missingProductIds.Count > 0)
		{
			return Result.Failure<CreateOrderResponse>(new Error(
				"Product.NotFound",
				$"Product(s) not found: {string.Join(", ", missingProductIds.Select(id => id.Value))}",
				ErrorType.NotFound));
		}

		var order = Order.Create(request.CustomerId);

		foreach (var item in request.Items)
		{
			var product = products[new ProductId(item.ProductId)];

			order.AddItem(product.Id, item.Quantity, Money.Create(product.Price));
		}

		dbContext.Orders.Add(order);

		await dbContext.SaveChangesAsync(cancellationToken);

		return new CreateOrderResponse(
			order.Id.Value,
			order.CustomerId,
			order.Status.ToString(),
			order.Items
				.Select(i => new CreateOrderResponseItem(i.ProductId.Value, i.Quantity, i.UnitPrice.Amount, i.TotalPrice.Amount))
				.ToList(),
			order.TotalAmount.Amount,
			order.TotalAmount.Currency);
	}
}
