using VerticalSliceArchitecture.Api.Common.Database;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;
using VerticalSliceArchitecture.Api.Domain.Entities;

namespace VerticalSliceArchitecture.Api.Features.Products.CreateProduct;

public sealed class CreateProductHandler(AppDbContext dbContext) : IRequestHandler<CreateProductCommand, Result<CreateProductResponse>>
{
	public async Task<Result<CreateProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
	{
		var product = Product.Create(request.Name, request.Price);

		dbContext.Products.Add(product);

		await dbContext.SaveChangesAsync(cancellationToken);

		return new CreateProductResponse(
			product.Id.Value,
			product.Name,
			product.Price);
	}
}
