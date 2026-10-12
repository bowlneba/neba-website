using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.UpdateUser;

namespace Neba.Api.Security.UpdateUser;

internal sealed class UpdateUserRequestValidator : Validator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(r => r.UserId)
            .Must(id => Ulid.TryParse(id, out _))
            .WithErrorCode("UpdateUserRequest.UserIdInvalid")
            .WithMessage("UserId must be a valid ULID.");

        this.ApplyPermissionOverrideRules(
            r => r.User.AddedPermissions,
            r => r.User.RemovedPermissions,
            "UpdateUserRequest");
    }
}