namespace VerticalSliceArchitecture.Api.Features.Products.CreateProduct;

public record CreateProductResponse(
	Guid Id,
	string Name,
	decimal Price);