using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Features.Products.CreateProduct;

public sealed record CreateProductCommand(string Name, decimal Price) : IRequest<Result<CreateProductResponse>>;
