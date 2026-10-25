using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.ChangePassword;

namespace Neba.Api.Security.Password.ChangePassword;

internal sealed class ChangePasswordRequestValidator : Validator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(r => r.CurrentPassword)
            .NotEmpty()
            .WithErrorCode("ChangePasswordRequest.CurrentPasswordRequired")
            .WithMessage("Current password is required.");

        // Same structural rules as SetPasswordFromTokenRequestValidator.
        RuleFor(r => r.NewPassword)
            .NotEmpty()
            .WithErrorCode("ChangePasswordRequest.NewPasswordRequired")
            .WithMessage("New password is required.")
            .MinimumLength(8)
            .WithErrorCode("ChangePasswordRequest.NewPasswordTooShort")
            .WithMessage("Password must be at least 8 characters.")
            .Matches(@"\d")
            .WithErrorCode("ChangePasswordRequest.NewPasswordRequiresDigit")
            .WithMessage("Password must contain at least one digit.");
    }
}