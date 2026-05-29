namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Tracking;

public class CreateTrackingRequestValidator : AbstractValidator<CreateTrackingRequest>
{
    public CreateTrackingRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.TargetPrice)
            .GreaterThan(0).WithMessage("Target price must be greater than zero.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .MaximumLength(10).WithMessage("Currency code must not exceed 10 characters.");
    }
}