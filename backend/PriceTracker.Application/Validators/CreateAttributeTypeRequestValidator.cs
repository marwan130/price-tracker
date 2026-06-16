namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Attributes;

public class CreateAttributeTypeRequestValidator : AbstractValidator<CreateAttributeTypeRequest>
{
    public CreateAttributeTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
    }
}
