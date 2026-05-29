namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Products;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(300).WithMessage("Product name must not exceed 300 characters.");

        RuleFor(x => x.Brand)
            .MaximumLength(150).WithMessage("Brand must not exceed 150 characters.")
            .When(x => x.Brand is not null);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Category ID must be a positive number.")
            .When(x => x.CategoryId.HasValue);
    }
}