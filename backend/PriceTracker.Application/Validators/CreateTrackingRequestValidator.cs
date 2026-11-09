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
            .Length(3).WithMessage("Currency code must be a three-letter ISO code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Currency code must contain letters only.");
    }
}
