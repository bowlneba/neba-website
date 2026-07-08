using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using Neba.Api.Database;
using Neba.Api.Messaging;

namespace Neba.Api.Features.News.DeleteArticle;

internal sealed class DeleteArticleCommandHandler(
    AppDbContext appDbContext,
    HybridCache cache)
        : ICommandHandler<DeleteArticleCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(DeleteArticleCommand command, CancellationToken cancellationToken)
    {
        var article = await appDbContext.Articles
            .SingleOrDefaultAsync(a => a.Id == command.ArticleId, cancellationToken);

        if (article is null)
        {
            return Result.Deleted;
        }

        appDbContext.Articles.Remove(article);
        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:news:articles", cancellationToken);
        await cache.RemoveByTagAsync($"neba:news:{article.Slug}", cancellationToken);

        return Result.Deleted;
    }
}
