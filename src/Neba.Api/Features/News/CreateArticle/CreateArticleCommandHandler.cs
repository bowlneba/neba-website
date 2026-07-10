using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.News.Domain;
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
        if (command.TournamentId is not null)
        {
            var tournamentExists = await appDbContext.Tournaments.AnyAsync(tournament => tournament.Id == command.TournamentId, cancellationToken);

            if (!tournamentExists)
            {
                return ArticleErrors.TournamentNotFound(command.TournamentId.Value);
            }
        }

        var articleResult = Article.Create(
            command.Title,
            command.Slug,
            command.Content,
            command.PublicationStatus,
            command.PublishDate.ToUniversalTime(),
            command.TournamentId);

        if (articleResult.IsError)
        {
            return articleResult.Errors;
        }

        var article = articleResult.Value;

        var slugExists = await appDbContext.Articles.AnyAsync(a => a.Slug == article.Slug, cancellationToken);

        if (slugExists)
        {
            return ArticleErrors.SlugAlreadyExists(article.Slug);
        }

        await appDbContext.Articles.AddAsync(article, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:news:articles", token: cancellationToken);

        return new CreatedArticle
        {
            Id = article.Id,
            Slug = article.Slug
        };
    }
}