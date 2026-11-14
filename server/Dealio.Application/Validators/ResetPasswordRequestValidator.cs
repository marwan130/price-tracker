namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Auth;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .ApplyPasswordRules();
    }
}
