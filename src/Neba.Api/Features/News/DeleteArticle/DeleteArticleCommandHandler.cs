using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.News.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.News.DeleteArticle;

internal sealed class DeleteArticleCommandHandler(
    AppDbContext appDbContext,
    IBackgroundJobScheduler backgroundJobScheduler,
    IFusionCache cache)
        : ICommandHandler<DeleteArticleCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(DeleteArticleCommand command, CancellationToken cancellationToken)
    {
        var article = await appDbContext.Articles
            .Include(article => article.Attachments)
            .SingleOrDefaultAsync(a => a.Id == command.ArticleId, cancellationToken);

        if (article is null)
        {
            return Result.Deleted;
        }

        var filesToDelete = BuildFileReferences(article);

        appDbContext.Articles.Remove(article);
        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:news:articles", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:news:{article.Slug}", token: cancellationToken);

        if (filesToDelete.Count > 0)
        {
            backgroundJobScheduler.Enqueue(new DeleteArticleFilesJob
            {
                Files = filesToDelete
            });
        }

        return Result.Deleted;
    }

    private static List<StoredFileReference> BuildFileReferences(Article article)
    {
        List<StoredFileReference> files = [];

        if (article.HeaderImage is not null)
        {
            files.Add(new()
            {
                Container = article.HeaderImage.Container,
                Path = article.HeaderImage.Path
            });
        }

        files.AddRange(article.Attachments.Select(attachment => new StoredFileReference
        {
            Container = attachment.File.Container,
            Path = attachment.File.Path
        }));

        return files;
    }
}