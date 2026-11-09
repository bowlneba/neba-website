using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Security;
using Neba.Api.Storage;

namespace Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;

internal sealed class AnnounceOilPatternToDiscordJobHandler(
    AppDbContext appDbContext,
    TimeProvider timeProvider,
    WebsiteSettings websiteSettings,
    IFileStorageService fileStorageService,
    INewsAnnouncementNotifier newsAnnouncementNotifier,
    ILogger<AnnounceOilPatternToDiscordJobHandler> logger)
    : IBackgroundJobHandler<AnnounceOilPatternToDiscordJob>
{
    public async Task ExecuteAsync(AnnounceOilPatternToDiscordJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        var tournamentId = job.TournamentId.ToString();

        // The only place that decides whether to post: one atomic statement, so concurrent jobs produce
        // one post and an edit after the job was scheduled still blocks it. Never undone: a failed post
        // is not retried, because a timeout after Discord accepted it looks the same as a failure.
        // Tournament.MarkOilPatternAnnounced is the same rule for in-memory callers.
        var claimed = await appDbContext.Tournaments
            .Where(tournament => tournament.Id == job.TournamentId
                && tournament.OilPatternRevealDateTime == job.ExpectedRevealDateTime
                && tournament.OilPatternAnnouncedAtUtc == null
                && tournament.OilPatterns.Any())
            .ExecuteUpdateAsync(
                set => set.SetProperty(tournament => tournament.OilPatternAnnouncedAtUtc, timeProvider.GetUtcNow()),
                cancellationToken);

        if (claimed == 0)
        {
            await LogSkipReasonAsync(job, tournamentId, cancellationToken);
            return;
        }

        var row = await appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournament.Id == job.TournamentId)
            .Select(tournament => new TournamentRow
            {
                Name = tournament.Name,
                LogoContainer = tournament.Logo != null ? tournament.Logo.Container : null,
                LogoPath = tournament.Logo != null ? tournament.Logo.Path : null,
                Sponsors = tournament.Sponsors
                    .Select(tournamentSponsor => new SponsorLogoRow
                    {
                        TitleSponsor = tournamentSponsor.TitleSponsor,
                        LogoContainer = tournamentSponsor.Sponsor.Logo != null ? tournamentSponsor.Sponsor.Logo.Container : null,
                        LogoPath = tournamentSponsor.Sponsor.Logo != null ? tournamentSponsor.Sponsor.Logo.Path : null
                    }).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        // Deleted between the claim and this read: nothing left to announce.
        if (row is null)
        {
            logger.LogAnnouncementSkippedMissing(tournamentId);
            return;
        }

        var (logoContainer, logoPath) = TournamentLogoResolver.Resolve(
            row.LogoContainer,
            row.LogoPath,
            row.Sponsors.Select(s => (s.TitleSponsor, s.LogoContainer, s.LogoPath)));

        Uri? logoUrl = logoContainer is not null && logoPath is not null
            ? fileStorageService.GetBlobUri(logoContainer, logoPath)
            : null;

        await newsAnnouncementNotifier.NotifyAsync(
            OilPatternAnnouncementMessage.Create(row.Name, tournamentId, websiteSettings.BaseUrl, logoUrl),
            cancellationToken);

        logger.LogOilPatternAnnounced(tournamentId);
    }

    // Reads the tournament only to say why the claim changed no row. It never decides whether to post.
    private async Task LogSkipReasonAsync(AnnounceOilPatternToDiscordJob job, string tournamentId, CancellationToken cancellationToken)
    {
        var state = await appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournament.Id == job.TournamentId)
            .Select(tournament => new SkipState
            {
                RevealDateTime = tournament.OilPatternRevealDateTime,
                AnnouncedAtUtc = tournament.OilPatternAnnouncedAtUtc,
                HasOilPattern = tournament.OilPatterns.Any()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (state is null)
        {
            logger.LogAnnouncementSkippedMissing(tournamentId);
        }
        else if (state.RevealDateTime is not { } revealDateTime)
        {
            logger.LogAnnouncementSkippedNoRevealDate(tournamentId);
        }
        else if (revealDateTime != job.ExpectedRevealDateTime)
        {
            logger.LogAnnouncementSkippedStaleDate(tournamentId, job.ExpectedRevealDateTime, revealDateTime);
        }
        else if (!state.HasOilPattern)
        {
            logger.LogAnnouncementSkippedNoPattern(tournamentId);
        }
        else if (state.AnnouncedAtUtc is not null)
        {
            logger.LogAnnouncementSkippedAlreadyAnnounced(tournamentId);
        }
        else
        {
            logger.LogAnnouncementSkippedClaimLost(tournamentId);
        }
    }

    private sealed class SkipState
    {
        public DateTimeOffset? RevealDateTime { get; init; }
        public DateTimeOffset? AnnouncedAtUtc { get; init; }
        public required bool HasOilPattern { get; init; }
    }

    private sealed class TournamentRow
    {
        public required string Name { get; init; }
        public string? LogoContainer { get; init; }
        public string? LogoPath { get; init; }
        public required List<SponsorLogoRow> Sponsors { get; init; }
    }

    private sealed class SponsorLogoRow
    {
        public required bool TitleSponsor { get; init; }
        public string? LogoContainer { get; init; }
        public string? LogoPath { get; init; }
    }
}

internal static partial class AnnounceOilPatternToDiscordJobLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: it no longer exists.")]
    public static partial void LogAnnouncementSkippedMissing(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: it has no reveal date.")]
    public static partial void LogAnnouncementSkippedNoRevealDate(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: the job was scheduled for {ExpectedRevealDateTime} but the reveal date is now {RevealDateTime}.")]
    public static partial void LogAnnouncementSkippedStaleDate(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId, DateTimeOffset expectedRevealDateTime, DateTimeOffset revealDateTime);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: no oil pattern is assigned.")]
    public static partial void LogAnnouncementSkippedNoPattern(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: it was already announced.")]
    public static partial void LogAnnouncementSkippedAlreadyAnnounced(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: another job claimed it, or it changed after the check.")]
    public static partial void LogAnnouncementSkippedClaimLost(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Announced the oil pattern for tournament {TournamentId} to Discord.")]
    public static partial void LogOilPatternAnnounced(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);
}