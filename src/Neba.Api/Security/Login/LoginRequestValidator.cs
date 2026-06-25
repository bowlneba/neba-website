using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.Login;

namespace Neba.Api.Security.Login;

internal sealed class LoginRequestValidator : Validator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty()
            .WithErrorCode("LoginRequest.EmailRequired")
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithErrorCode("LoginRequest.EmailInvalid")
            .WithMessage("A valid email address is required.");

        RuleFor(r => r.Password)
            .NotEmpty()
            .WithErrorCode("LoginRequest.PasswordRequired")
            .WithMessage("Password is required.");
    }
}
