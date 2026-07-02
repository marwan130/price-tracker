namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Password)
            .ApplyPasswordRules();

        RuleFor(x => x.Phone)
            .MaximumLength(13).WithMessage("Phone must not exceed 13 characters.")
            .When(x => x.Phone is not null);
    }
}