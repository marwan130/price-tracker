namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Listings;

public class CreateListingRequestValidator : AbstractValidator<CreateListingRequest>
{
    public CreateListingRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("Variant ID is required.");

        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("Store ID is required.");

        RuleFor(x => x.ProductUrl)
            .NotEmpty().WithMessage("Product URL is required.")
            .MaximumLength(1000).WithMessage("Product URL must not exceed 1000 characters.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Product URL must be a valid URL.");
    }
}