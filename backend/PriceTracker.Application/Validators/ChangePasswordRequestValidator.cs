namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Auth;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .ApplyPasswordRules()
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from the current password.");
    }
}
