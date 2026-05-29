namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Variants;

public class CreateVariantRequestValidator : AbstractValidator<CreateVariantRequest>
{
    public CreateVariantRequestValidator()
    {
        RuleFor(x => x.Sku)
            .MaximumLength(200).WithMessage("SKU must not exceed 200 characters.")
            .When(x => x.Sku is not null);

        RuleFor(x => x.AttributeValueIds)
            .NotEmpty().WithMessage("At least one attribute value is required.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Attribute value IDs must be unique.");
    }
}