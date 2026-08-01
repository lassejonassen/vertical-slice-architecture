using VerticalSliceArchitecture.Api.Common.Endpoints;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Features.Products.GetProductById;

public sealed class GetProductByIdEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet($"{ProductsConstants.BaseRoute}/{{id:guid}}", async (
			Guid id,
			IMediator mediator,
			CancellationToken cancellationToken) =>
		{
			var query = new GetProductByIdQuery(id);

			var result = await mediator.Send(query, cancellationToken);

			return result.IsSuccess
				? Results.Ok(result.Value)
				: result.ToProblem();
		})
		.WithName("GetProductById")
		.WithTags(ProductsConstants.Tag)
		.Produces<ProductDetailsDto>(StatusCodes.Status200OK)
		.ProducesProblem(StatusCodes.Status404NotFound);
	}
}
