using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Features.Products.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDetailsDto>>;
