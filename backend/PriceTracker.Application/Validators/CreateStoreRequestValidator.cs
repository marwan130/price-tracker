namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Stores;

public class CreateStoreRequestValidator : AbstractValidator<CreateStoreRequest>
{
    public CreateStoreRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Store name is required.")
            .MaximumLength(150).WithMessage("Store name must not exceed 150 characters.");

        RuleFor(x => x.BaseUrl)
            .NotEmpty().WithMessage("Base URL is required.")
            .MaximumLength(500).WithMessage("Base URL must not exceed 500 characters.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Base URL must be a valid URL.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");

        RuleFor(x => x.CurrencyCode)
            .MaximumLength(10).WithMessage("Currency code must not exceed 10 characters.")
            .When(x => x.CurrencyCode is not null);
    }
}