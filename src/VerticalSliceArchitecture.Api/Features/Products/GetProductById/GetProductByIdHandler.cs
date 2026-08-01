using Microsoft.EntityFrameworkCore;
using VerticalSliceArchitecture.Api.Common.Database;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;
using VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;

namespace VerticalSliceArchitecture.Api.Features.Products.GetProductById;

public sealed class GetProductByIdHandler(AppDbContext dbContext) : IRequestHandler<GetProductByIdQuery, Result<ProductDetailsDto>>
{
	public async Task<Result<ProductDetailsDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
	{
		var productId = new ProductId(request.Id);

		var product = await dbContext.Products
			.AsNoTracking()
			.Where(p => p.Id == productId)
			.Select(p => new ProductDetailsDto(p.Id.Value, p.Name, p.Price))
			.FirstOrDefaultAsync(cancellationToken);

		if (product is null)
		{
			return Result.Failure<ProductDetailsDto>(
				new Error("Product.NotFound", $"Product with Id '{request.Id}' was not found.", ErrorType.NotFound));
		}

		return product;
	}
}
