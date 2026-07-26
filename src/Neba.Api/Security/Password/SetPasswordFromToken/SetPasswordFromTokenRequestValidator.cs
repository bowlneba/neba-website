using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.SetPasswordFromToken;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed class SetPasswordFromTokenRequestValidator : Validator<SetPasswordFromTokenRequest>
{
    public SetPasswordFromTokenRequestValidator()
    {
        RuleFor(r => r.UserId)
            .NotEmpty()
            .WithErrorCode("SetPasswordFromTokenRequest.UserIdRequired")
            .WithMessage("User ID is required.")
            .Must(id => Ulid.TryParse(id, out _))
            .WithErrorCode("SetPasswordFromTokenRequest.UserIdInvalid")
            .WithMessage("User ID must be a valid ULID.");

        RuleFor(r => r.Token)
            .NotEmpty()
            .WithErrorCode("SetPasswordFromTokenRequest.TokenRequired")
            .WithMessage("Token is required.");

        RuleFor(r => r.NewPassword)
            .NotEmpty()
            .WithErrorCode("SetPasswordFromTokenRequest.NewPasswordRequired")
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithErrorCode("SetPasswordFromTokenRequest.NewPasswordTooShort")
            .WithMessage("Password must be at least 8 characters.")
            .Matches(@"\d")
            .WithErrorCode("SetPasswordFromTokenRequest.NewPasswordRequiresDigit")
            .WithMessage("Password must contain at least one digit.");
    }
}
