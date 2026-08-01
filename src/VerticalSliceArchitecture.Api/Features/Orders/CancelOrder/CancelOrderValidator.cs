using FluentValidation;

namespace VerticalSliceArchitecture.Api.Features.Orders.CancelOrder;

public sealed class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
	public CancelOrderValidator()
	{
		RuleFor(x => x.OrderId)
			.NotEmpty().WithMessage("Order Id is required.");
	}
}
