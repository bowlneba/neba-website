using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Security.ListUsers;

internal sealed class ListUsersRequestValidator : Validator<ListUsersRequest>
{
    public ListUsersRequestValidator()
    {
        RuleFor(r => r.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("ListUsersRequest.PageInvalid")
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(r => r.PageSize)
            .InclusiveBetween(1, 100)
            .WithErrorCode("ListUsersRequest.PageSizeInvalid")
            .WithMessage("Page size must be between 1 and 100.");
    }
}