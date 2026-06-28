using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.RefreshToken;

namespace Neba.Api.Security.RefreshToken;

internal sealed class RefreshTokenRequestValidator : Validator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(r => r.UserId)
            .NotEmpty()
            .WithErrorCode("RefreshTokenRequest.UserIdRequired")
            .WithMessage("User ID is required.")
            .Must(v => Ulid.TryParse(v, out _))
            .WithErrorCode("RefreshTokenRequest.UserIdInvalid")
            .WithMessage("User ID must be a valid ULID.");

        RuleFor(r => r.RefreshToken)
            .NotEmpty()
            .WithErrorCode("RefreshTokenRequest.RefreshTokenRequired")
            .WithMessage("Refresh token is required.");
    }
}
