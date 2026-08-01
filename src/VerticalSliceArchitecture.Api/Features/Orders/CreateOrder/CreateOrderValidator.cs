using FluentValidation;

namespace VerticalSliceArchitecture.Api.Features.Orders.CreateOrder;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
	public CreateOrderValidator()
	{
		RuleFor(x => x.CustomerId)
			.NotEmpty().WithMessage("Customer Id is required.");

		RuleFor(x => x.Items)
			.NotEmpty().WithMessage("Order must contain at least one item.");

		RuleForEach(x => x.Items).SetValidator(new CreateOrderItemValidator());
	}
}

public sealed class CreateOrderItemValidator : AbstractValidator<CreateOrderCommandItem>
{
	public CreateOrderItemValidator()
	{
		RuleFor(x => x.ProductId)
			.NotEmpty().WithMessage("Product Id is required.");

		RuleFor(x => x.Quantity)
			.GreaterThan(0).WithMessage("Quantity must be greater than 0.");
	}
}
