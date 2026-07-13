using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.News.CreateArticle;
using Neba.Api.Features.News.DeleteArticle;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.News.EditArticle;

internal sealed class EditArticleCommandHandler(
        AppDbContext appDbContext,
        IBackgroundJobScheduler backgroundJobScheduler,
        IFusionCache cache)
    : ICommandHandler<EditArticleCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditArticleCommand command, CancellationToken cancellationToken)
    {
        var article = await appDbContext.Articles
        .Include(a => a.Attachments)
        .SingleOrDefaultAsync(a => a.Id == command.ArticleId, cancellationToken);

        if (article is null)
        {
            return ArticleErrors.ArticleNotFound(command.ArticleId.Value.ToString());
        }

        var tournamentCheckResult = await EnsureTournamentExistsAsync(command.TournamentId, cancellationToken);

        if (tournamentCheckResult.IsError)
        {
            return tournamentCheckResult.Errors;
        }

        var sanitizedContent = HtmlContentSanitizer.Sanitize(command.Content);

        // Must snapshot before Update() — HeaderImage is mutated in place, so reading it after the
        // call would return the new value, not the one being replaced.
        var previousHeaderImage = article.HeaderImage;

        var updateResult = article.Update(
            command.Title,
            sanitizedContent,
            command.PublicationStatus,
            command.PublishDate.ToUniversalTime(),
            command.TournamentId,
            command.HeaderImage);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        var attachmentsResult = ReconcileAttachments(article, command.Attachments);

        if (attachmentsResult.IsError)
        {
            return attachmentsResult.Errors;
        }

        var orphanedFiles = attachmentsResult.Value;

        if (previousHeaderImage is not null && previousHeaderImage != command.HeaderImage)
        {
            orphanedFiles.Add(new StoredFileReference
            {
                Container = previousHeaderImage.Container,
                Path = previousHeaderImage.Path
            });
        }

        await RemoveClaimedPendingUploadsAsync(command, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:news:articles", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:news:{article.Slug}", token: cancellationToken);

        if (orphanedFiles.Count > 0)
        {
            backgroundJobScheduler.Enqueue(new DeleteArticleFilesJob
            {
                Files = orphanedFiles
            });
        }

        return Result.Updated;
    }

    private async Task<ErrorOr<Success>> EnsureTournamentExistsAsync(TournamentId? tournamentId, CancellationToken cancellationToken)
    {
        if (tournamentId is null)
        {
            return Result.Success;
        }

        var tournamentExists = await appDbContext.Tournaments.AnyAsync(tournament => tournament.Id == tournamentId, cancellationToken);

        return tournamentExists
            ? Result.Success
            : ArticleErrors.TournamentNotFound(tournamentId.Value);
    }

    private static ErrorOr<List<StoredFileReference>> ReconcileAttachments(
        Article article,
        IReadOnlyCollection<EditArticleAttachment> desiredAttachments)
    {
        List<StoredFileReference> orphanedFiles = [];

        var desiredKeys = desiredAttachments
            .Select(a => (a.File.Container, a.File.Path))
            .ToHashSet();

        var toRemove = article.Attachments
            .Where(existing => !desiredKeys.Contains((existing.File.Container, existing.File.Path)))
            .ToList();

        foreach (var existing in toRemove)
        {
            var removed = article.RemoveAttachment(existing.Id);

            if (removed.IsError)
            {
                return removed.Errors;
            }

            orphanedFiles.Add(new StoredFileReference
            {
                Container = existing.File.Container,
                Path = existing.File.Path
            });
        }

        var existingKeys = article.Attachments
            .Select(a => (a.File.Container, a.File.Path))
            .ToHashSet();

        var toAdd = desiredAttachments
            .Where(desired => !existingKeys.Contains((desired.File.Container, desired.File.Path)));

        foreach (var attachment in toAdd)
        {
            var added = article.AddAttachment(attachment.DisplayName, attachment.File, attachment.IsInline);

            if (added.IsError)
            {
                return added.Errors;
            }
        }

        return orphanedFiles;
    }

    private async Task RemoveClaimedPendingUploadsAsync(EditArticleCommand command, CancellationToken cancellationToken)
    {
        var claimedFiles = command.Attachments
            .Select(attachment => attachment.File)
            .Concat(command.HeaderImage is null ? [] : [command.HeaderImage])
            .ToList();

        if (claimedFiles.Count == 0)
        {
            return;
        }

        var claimedContainers = claimedFiles.Select(file => file.Container).Distinct().ToList();

        var candidates = await appDbContext.PendingUploads
            .Where(pending => claimedContainers.Contains(pending.Container))
            .ToListAsync(cancellationToken);

        var claimedPaths = claimedFiles.Select(file => (file.Container, file.Path)).ToHashSet();
        var claimed = candidates.Where(pending => claimedPaths.Contains((pending.Container, pending.Path)));

        appDbContext.PendingUploads.RemoveRange(claimed);
    }
}