using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Features.Orders.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId) : IRequest<Result>;
