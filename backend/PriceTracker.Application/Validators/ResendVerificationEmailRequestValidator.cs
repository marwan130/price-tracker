namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Auth;

public class ResendVerificationEmailRequestValidator : AbstractValidator<ResendVerificationEmailRequest>
{
    public ResendVerificationEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");
    }
}
