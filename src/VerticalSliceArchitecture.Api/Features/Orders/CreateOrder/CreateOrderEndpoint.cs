using VerticalSliceArchitecture.Api.Common.Endpoints;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Features.Orders.CreateOrder;

public sealed class CreateOrderEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost(OrdersConstants.BaseRoute, async (
			CreateOrderRequest request,
			IMediator mediator,
			CancellationToken cancellationToken) =>
		{
			var command = new CreateOrderCommand(
				request.CustomerId,
				request.Items.Select(i => new CreateOrderCommandItem(i.ProductId, i.Quantity)).ToList());

			var result = await mediator.Send(command, cancellationToken);

			return result.IsSuccess
				? Results.Created($"{OrdersConstants.BaseRoute}/{result.Value.Id}", result.Value)
				: result.ToProblem();
		})
		.WithName("CreateOrder")
		.WithTags(OrdersConstants.Tag)
		.Produces<CreateOrderResponse>(StatusCodes.Status201Created)
		.ProducesProblem(StatusCodes.Status400BadRequest)
		.ProducesProblem(StatusCodes.Status404NotFound);
	}
}

// Request contract matching incoming HTTP payload
public sealed record CreateOrderRequest(Guid CustomerId, List<CreateOrderRequestItem> Items);
public sealed record CreateOrderRequestItem(Guid ProductId, int Quantity);
