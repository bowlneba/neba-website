using FastEndpoints;

using FluentValidation;

using Neba.Api.Features.News.Domain;

namespace Neba.Api.Features.News.EditArticle;

internal sealed class EditArticleRequestValidator
    : Validator<EditArticleRequest>
{
    public EditArticleRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("EditArticleRequest.IdRequired")
            .WithMessage("Id is required.")
            .Length(26)
            .WithErrorCode("EditArticleRequest.IdInvalidLength")
            .WithMessage("Id must be a 26-character ULID.");

        RuleFor(r => r.Article.Title)
            .NotEmpty()
            .WithErrorCode("EditArticleRequest.TitleRequired")
            .WithMessage("Title is required.")
            .MaximumLength(256)
            .WithErrorCode("EditArticleRequest.TitleTooLong")
            .WithMessage("Title must be 256 characters or fewer.");

        RuleFor(r => r.Article.Content)
            .NotEmpty()
            .WithErrorCode("EditArticleRequest.ContentRequired")
            .WithMessage("Content is required.");

        RuleFor(r => r.Article.PublicationStatus)
            .NotEmpty()
            .WithErrorCode("EditArticleRequest.PublicationStatusRequired")
            .WithMessage("Publication status is required.")
            .Must(status => PublicationStatus.List.Any(s => s.Name == status))
            .WithErrorCode("EditArticleRequest.PublicationStatusInvalid")
            .WithMessage("Publication status must be one of: Draft, Published.");

        RuleFor(r => r.Article.PublishDate)
            .NotEqual(default(DateTimeOffset))
            .WithErrorCode("EditArticleRequest.PublishDateRequired")
            .WithMessage("Publish date is required.");

        RuleFor(r => r.Article.TournamentId)
            .Length(26)
            .WithErrorCode("EditArticleRequest.TournamentIdInvalidLength")
            .WithMessage("TournamentId must be a 26-character ULID.")
            .When(r => !string.IsNullOrWhiteSpace(r.Article.TournamentId));
    }
}
