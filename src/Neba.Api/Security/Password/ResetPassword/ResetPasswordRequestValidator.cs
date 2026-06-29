using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.ResetPassword;

namespace Neba.Api.Security.Password.ResetPassword;

internal sealed class ResetPasswordRequestValidator : Validator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(r => r.UserId)
            .NotEmpty()
            .WithErrorCode("ResetPasswordRequest.UserIdRequired")
            .WithMessage("User ID is required.")
            .Must(id => Ulid.TryParse(id, out _))
            .WithErrorCode("ResetPasswordRequest.UserIdInvalid")
            .WithMessage("User ID must be a valid ULID.");
    }
}
