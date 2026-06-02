using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.News.ListArticles;

internal sealed class ListArticlesRequestValidator : Validator<ListArticlesRequest>
{
    public ListArticlesRequestValidator()
    {
        RuleFor(r => r.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("ListArticlesRequest.PageInvalid")
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(r => r.PageSize)
            .InclusiveBetween(1, 100)
            .WithErrorCode("ListArticlesRequest.PageSizeInvalid")
            .WithMessage("Page size must be between 1 and 100.");
    }
}