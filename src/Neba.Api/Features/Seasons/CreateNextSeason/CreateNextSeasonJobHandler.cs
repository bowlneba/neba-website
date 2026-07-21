using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;

namespace Neba.Api.Features.Seasons.CreateNextSeason;

internal sealed class CreateNextSeasonJobHandler(
        AppDbContext appDbContext,
        TimeProvider timeProvider,
        ILogger<CreateNextSeasonJobHandler> logger)
    : IBackgroundJobHandler<CreateNextSeasonJob>
{
    public async Task ExecuteAsync(CreateNextSeasonJob _, CancellationToken cancellationToken)
    {
        var nextYear = timeProvider.GetUtcNow().Year + 1;
        var endDate = new DateOnly(nextYear, 12, 31);

        var alreadyExists = await appDbContext.Seasons
            .AnyAsync(season => season.EndDate == endDate, cancellationToken);

        if (alreadyExists)
        {
            logger.LogSeasonAlreadyExists(nextYear);

            return;
        }

        var startDate = new DateOnly(nextYear, 1, 1);
        var seasonResult = Season.Create($"{nextYear} Season", startDate, endDate);

        if (seasonResult.IsError)
        {
            logger.LogSeasonCreationFailed(nextYear, string.Join(", ", seasonResult.Errors.Select(error => error.Code)));

            return;
        }

        await appDbContext.Seasons.AddAsync(seasonResult.Value, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        logger.LogSeasonCreated(nextYear);
    }
}

internal static partial class CreateNextSeasonJobHandlerLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Season for {Year} already exists; skipping creation.")]
    public static partial void LogSeasonAlreadyExists(this ILogger<CreateNextSeasonJobHandler> logger, int year);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Created {Year} season.")]
    public static partial void LogSeasonCreated(this ILogger<CreateNextSeasonJobHandler> logger, int year);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to create {Year} season: {ErrorCodes}.")]
    public static partial void LogSeasonCreationFailed(this ILogger<CreateNextSeasonJobHandler> logger, int year, string errorCodes);
}
