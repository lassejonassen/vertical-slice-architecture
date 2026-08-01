using VerticalSliceArchitecture.Api.Common.Endpoints;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Features.Orders.CancelOrder;

public sealed class CancelOrderEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost($"{OrdersConstants.BaseRoute}/{{id:guid}}/cancel", async (
			Guid id,
			IMediator mediator,
			CancellationToken cancellationToken) =>
		{
			var command = new CancelOrderCommand(id);

			var result = await mediator.Send(command, cancellationToken);

			return result.IsSuccess
				? Results.NoContent()
				: result.ToProblem();
		})
		.WithName("CancelOrder")
		.WithTags(OrdersConstants.Tag)
		.Produces(StatusCodes.Status204NoContent)
		.ProducesProblem(StatusCodes.Status404NotFound)
		.ProducesProblem(StatusCodes.Status409Conflict);
	}
}
