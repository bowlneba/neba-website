using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.News.CreateArticle;
using Neba.Api.Features.News.Domain;

namespace Neba.Api.Features.News.CreateArticle;

internal sealed class CreateArticleRequestValidator
    : Validator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(r => r.Article.Title)
            .NotEmpty()
            .WithErrorCode("CreateArticleRequest.TitleRequired")
            .WithMessage("Title is required.")
            .MaximumLength(256)
            .WithErrorCode("CreateArticleRequest.TitleTooLong")
            .WithMessage("Title must be 256 characters or fewer.");

        RuleFor(r => r.Article.Slug)
            .MaximumLength(256)
            .WithErrorCode("CreateArticleRequest.SlugTooLong")
            .WithMessage("Slug must be 256 characters or fewer.")
            .When(r => !string.IsNullOrWhiteSpace(r.Article.Slug));

        RuleFor(r => r.Article.Content)
            .NotEmpty()
            .WithErrorCode("CreateArticleRequest.ContentRequired")
            .WithMessage("Content is required.");

        RuleFor(r => r.Article.PublicationStatus)
            .NotEmpty()
            .WithErrorCode("CreateArticleRequest.PublicationStatusRequired")
            .WithMessage("Publication status is required.")
            .Must(status => PublicationStatus.List.Any(s => s.Name == status))
            .WithErrorCode("CreateArticleRequest.PublicationStatusInvalid")
            .WithMessage("Publication status must be one of: Draft, Published.");

        RuleFor(r => r.Article.PublishDateUtc)
            .NotEqual(default(DateTimeOffset))
            .WithErrorCode("CreateArticleRequest.PublishDateRequired")
            .WithMessage("Publish date is required.");

        RuleFor(r => r.Article.TournamentId)
            .Length(26)
            .WithErrorCode("CreateArticleRequest.TournamentIdInvalidLength")
            .WithMessage("TournamentId must be a 26-character ULID.")
            .When(r => !string.IsNullOrWhiteSpace(r.Article.TournamentId));
    }
}
