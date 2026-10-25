using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.ForgotPassword;

namespace Neba.Api.Security.Password.ForgotPassword;

internal sealed class ForgotPasswordRequestValidator : Validator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty()
            .WithErrorCode("ForgotPasswordRequest.EmailRequired")
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithErrorCode("ForgotPasswordRequest.EmailInvalid")
            .WithMessage("A valid email address is required.")
            .MaximumLength(256)
            .WithErrorCode("ForgotPasswordRequest.EmailTooLong")
            .WithMessage("Email must be 256 characters or fewer.");
    }
}
