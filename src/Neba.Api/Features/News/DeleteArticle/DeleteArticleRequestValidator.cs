using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.News.DeleteArticle;

internal sealed class DeleteArticleRequestValidator
    : Validator<DeleteArticleRequest>
{
    public DeleteArticleRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("DeleteArticleRequest.IdRequired")
            .WithMessage("Id is required.")
            .Length(26)
            .WithErrorCode("DeleteArticleRequest.IdInvalidLength")
            .WithMessage("Id must be a 26-character ULID.");
    }
}
