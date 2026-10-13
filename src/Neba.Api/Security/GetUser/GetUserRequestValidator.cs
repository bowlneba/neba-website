using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.GetUser;

namespace Neba.Api.Security.GetUser;

internal sealed class GetUserRequestValidator : Validator<GetUserRequest>
{
    public GetUserRequestValidator()
    {
        RuleFor(r => r.UserId)
            .Must(id => Ulid.TryParse(id, out _))
            .WithErrorCode("GetUserRequest.UserIdInvalid")
            .WithMessage("UserId must be a valid ULID.");
    }
}