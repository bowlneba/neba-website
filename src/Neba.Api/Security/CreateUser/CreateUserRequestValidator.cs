using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.CreateUser;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.CreateUser;

internal sealed class CreateUserRequestValidator : Validator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(r => r.User.Email)
            .NotEmpty()
            .WithErrorCode("CreateUserRequest.EmailRequired")
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithErrorCode("CreateUserRequest.EmailInvalid")
            .WithMessage("A valid email address is required.");

        RuleFor(r => r.User.Roles)
            .NotEmpty()
            .WithErrorCode("CreateUserRequest.RolesRequired")
            .WithMessage("At least one role is required.");

        RuleForEach(r => r.User.Roles)
            .Must(role => role != Roles.Admin)
            .WithErrorCode("CreateUserRequest.AdminRoleNotAllowed")
            .WithMessage("The Admin role cannot be granted through this endpoint.")
            .Must(role => Roles.All.Contains(role))
            .WithErrorCode("CreateUserRequest.RoleUnknown")
            .WithMessage("One or more roles are not recognized.");
    }
}