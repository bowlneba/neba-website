using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.News.CreateArticle;

internal sealed class CreateArticleCommandHandler(
        AppDbContext appDbContext,
        IFusionCache cache)
    : ICommandHandler<CreateArticleCommand, CreatedArticle>
{
    public async Task<ErrorOr<CreatedArticle>> HandleAsync(CreateArticleCommand command, CancellationToken cancellationToken)
    {
        var tournamentCheck = await EnsureTournamentExistsAsync(command.TournamentId, cancellationToken);

        if (tournamentCheck.IsError)
        {
            return tournamentCheck.Errors;
        }

        var articleResult = Article.Create(
            command.Title,
            command.Slug,
            command.Content,
            command.PublicationStatus,
            command.PublishDate.ToUniversalTime(),
            command.TournamentId,
            command.HeaderImage);

        if (articleResult.IsError)
        {
            return articleResult.Errors;
        }

        var article = articleResult.Value;

        var slugCheck = await EnsureSlugIsAvailableAsync(article.Slug, cancellationToken);

        if (slugCheck.IsError)
        {
            return slugCheck.Errors;
        }

        var attachmentsResult = AddAttachments(article, command.Attachments);

        if (attachmentsResult.IsError)
        {
            return attachmentsResult.Errors;
        }

        await appDbContext.Articles.AddAsync(article, cancellationToken);

        await RemoveClaimedPendingUploadsAsync(command, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:news:articles", token: cancellationToken);

        return new CreatedArticle
        {
            Id = article.Id,
            Slug = article.Slug
        };
    }

    private async Task<ErrorOr<Success>> EnsureTournamentExistsAsync(TournamentId? tournamentId, CancellationToken cancellationToken)
    {
        if (tournamentId is null)
        {
            return Result.Success;
        }

        var tournamentExists = await appDbContext.Tournaments
            .AnyAsync(tournament => tournament.Id == tournamentId, cancellationToken);

        return tournamentExists
            ? Result.Success
            : ArticleErrors.TournamentNotFound(tournamentId.Value);
    }

    private async Task<ErrorOr<Success>> EnsureSlugIsAvailableAsync(string slug, CancellationToken cancellationToken)
    {
        var slugExists = await appDbContext.Articles.AnyAsync(a => a.Slug == slug, cancellationToken);

        return slugExists
            ? ArticleErrors.SlugAlreadyExists(slug)
            : Result.Success;
    }

    private static ErrorOr<Success> AddAttachments(Article article, IReadOnlyCollection<NewArticleAttachment> attachments)
    {
        foreach (var attachment in attachments)
        {
            var added = article.AddAttachment(attachment.DisplayName, attachment.File, attachment.IsInline);

            if (added.IsError)
            {
                return added.Errors;
            }
        }

        return Result.Success;
    }

    private async Task RemoveClaimedPendingUploadsAsync(CreateArticleCommand command, CancellationToken cancellationToken)
    {
        var claimedFiles = command.Attachments
            .Select(attachment => attachment.File)
            .Concat(command.HeaderImage is null ? [] : [command.HeaderImage])
            .ToList();

        foreach (var file in claimedFiles)
        {
            var pending = await appDbContext.PendingUploads
                .FirstOrDefaultAsync(pending => pending.Container == file.Container
                    && pending.Path == file.Path, cancellationToken);

            if (pending is not null)
            {
                appDbContext.PendingUploads.Remove(pending);
            }
        }
    }
}