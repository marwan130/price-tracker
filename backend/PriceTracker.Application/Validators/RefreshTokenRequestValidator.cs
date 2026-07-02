namespace PriceTracker.Application.Validators;

using FluentValidation;
using PriceTracker.Application.DTOs.Auth;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
