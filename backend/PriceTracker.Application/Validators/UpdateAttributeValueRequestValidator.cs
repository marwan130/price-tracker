namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Attributes;

public class UpdateAttributeValueRequestValidator : AbstractValidator<UpdateAttributeValueRequest>
{
    public UpdateAttributeValueRequestValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Value is required.")
            .MaximumLength(100).WithMessage("Value must not exceed 100 characters.");
    }
}
