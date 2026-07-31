using FluentValidation;

namespace VerticalSliceArchitecture.Api.Features.Products.CreateProduct;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
	public CreateProductValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty().WithMessage("Product name is required.")
			.MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

		RuleFor(x => x.Price)
			.GreaterThan(0).WithMessage("Price must be greater than 0.");
	}
}
