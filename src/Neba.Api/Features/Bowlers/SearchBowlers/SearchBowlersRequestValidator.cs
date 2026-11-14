using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Bowlers.SearchBowlers;

internal sealed class SearchBowlersRequestValidator : Validator<SearchBowlersRequest>
{
    public SearchBowlersRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithErrorCode("SearchBowlersRequest.NameRequired")
            .WithMessage("Name is required.")
            .MinimumLength(3)
            .WithErrorCode("SearchBowlersRequest.NameTooShort")
            .WithMessage("Enter at least 3 characters.")
            .MaximumLength(100)
            .WithErrorCode("SearchBowlersRequest.NameTooLong")
            .WithMessage("Name is too long.");
    }
}