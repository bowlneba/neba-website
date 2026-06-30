using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.Register;

namespace Neba.Api.Security.Register;

internal sealed class RegisterRequestValidator : Validator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Input.Email)
            .NotEmpty()
            .WithErrorCode("RegisterRequest.EmailRequired")
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithErrorCode("RegisterRequest.EmailInvalid")
            .WithMessage("A valid email address is required.");

        RuleFor(r => r.Input.Password)
            .NotEmpty()
            .WithErrorCode("RegisterRequest.PasswordRequired")
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithErrorCode("RegisterRequest.PasswordTooShort")
            .WithMessage("Password must be at least 8 characters.")
            .Matches(@"\d")
            .WithErrorCode("RegisterRequest.PasswordRequiresDigit")
            .WithMessage("Password must contain at least one digit.");
    }
}
