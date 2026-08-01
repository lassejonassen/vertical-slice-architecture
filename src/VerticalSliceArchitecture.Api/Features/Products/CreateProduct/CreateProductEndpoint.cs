using VerticalSliceArchitecture.Api.Common.Endpoints;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Features.Products.CreateProduct;

public sealed class CreateProductEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost(ProductsConstants.BaseRoute, async (
			CreateProductRequest request,
			IMediator mediator,
			CancellationToken cancellationToken) =>
		{
			var command = new CreateProductCommand(request.Name, request.Price);

			var result = await mediator.Send(command, cancellationToken);

			return result.IsSuccess
				? Results.Created($"{ProductsConstants.BaseRoute}/{result.Value.Id}", result.Value)
				: result.ToProblem();
		})
		.WithName("CreateProduct")
		.WithTags(ProductsConstants.Tag)
		.Produces<CreateProductResponse>(StatusCodes.Status201Created)
		.ProducesProblem(StatusCodes.Status400BadRequest);
	}
}

// Request contract matching incoming HTTP payload
public record CreateProductRequest(string Name, decimal Price);