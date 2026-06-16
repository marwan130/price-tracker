namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Attributes;

public class UpdateAttributeTypeRequestValidator : AbstractValidator<UpdateAttributeTypeRequest>
{
    public UpdateAttributeTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
    }
}
