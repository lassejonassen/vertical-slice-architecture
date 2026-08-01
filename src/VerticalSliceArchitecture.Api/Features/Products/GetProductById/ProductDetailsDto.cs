namespace VerticalSliceArchitecture.Api.Features.Products.GetProductById;

public sealed record ProductDetailsDto(Guid Id, string Name, decimal Price);
