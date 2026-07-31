using VerticalSliceArchitecture.Api.Common.Endpoints;
using VerticalSliceArchitecture.Api.Common.Messaging;

namespace VerticalSliceArchitecture.Api.Features.Products.CreateProduct;

public sealed class CreateProductEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/api/products", async (
			CreateProductRequest request,
			IMediator mediator,
			CancellationToken cancellationToken) =>
		{
			var command = new CreateProductCommand(request.Name, request.Price);

			var response = await mediator.Send(command, cancellationToken);

			return Results.Created($"/api/products/{response.Value.Id}", response);
		})
		.WithName("CreateProduct")
		.WithTags("Products")
		.Produces<CreateProductResponse>(StatusCodes.Status201Created)
		.ProducesProblem(StatusCodes.Status400BadRequest);
	}
}

// Request contract matching incoming HTTP payload
public record CreateProductRequest(string Name, decimal Price);