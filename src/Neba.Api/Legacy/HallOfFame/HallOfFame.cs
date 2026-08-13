using System.Data;
using System.Globalization;
using System.Net;

using Dapper;

using FluentValidation;

using Hangfire;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Identity;

namespace Neba.Api.Legacy.HallOfFame;

internal static class NewHallOfFameInductionEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapNewHallOfFameInduction()
        {
            app.MapPost("/hall-of-fame/new", (
                NewHallOfFameInductionRequest request,
                IValidator<NewHallOfFameInductionRequest> validator,
                IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<NewHallOfFameInductionSyncJob>(job => job.SyncAsync(request.HallOfFameIds, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed record NewHallOfFameInductionRequest(IReadOnlyCollection<int> HallOfFameIds);

internal sealed class NewHallOfFameInductionRequestValidator
    : AbstractValidator<NewHallOfFameInductionRequest>
{
    public NewHallOfFameInductionRequestValidator()
    {
        RuleFor(x => x.HallOfFameIds)
            .NotEmpty();

        RuleForEach(x => x.HallOfFameIds)
            .GreaterThan(0);
    }
}

// Legacy-shaped construction/mutation kept off the aggregate itself (per CLAUDE.md's guidance on
// legacy-only extension members) since HallOfFameInduction is expected to grow a first-class
// creation/edit path later. Only these two members are deleted at sunset - the Categories setter
// visibility change on the aggregate itself stays, since a first-class "add category" method would
// need the same setter.
internal static class LegacyHallOfFameInductionExtensions
{
    extension(HallOfFameInduction)
    {
        public static HallOfFameInduction CreateFromLegacy(
            BowlerId bowlerId,
            int year,
            HallOfFameCategory category)
            => new()
            {
                Id = HallOfFameId.New(),
                BowlerId = bowlerId,
                Year = year,
                Categories = [category]
            };
    }

    extension(HallOfFameInduction induction)
    {
        public void ApplyLegacyCategory(HallOfFameCategory category)
        {
            if (induction.Categories.Contains(category))
            {
                return;
            }

            induction.Categories = [.. induction.Categories, category];
        }
    }
}

internal sealed class NewHallOfFameInductionSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IEmailSender emailSender,
    ILogger<NewHallOfFameInductionSyncJob> logger)
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2077:Use a parameterized query instead of string formatting.",
        Justification = "The interpolated segment is only generated placeholder names (@Id0, @Id1, ...), never a data value - every id value itself is bound as a real DynamicParameters entry, not concatenated into the SQL text.")]
    public async Task SyncAsync(IReadOnlyCollection<int> legacyHallOfFameIds, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        // Placeholders are numbered and bound individually (Id0, Id1, ...) rather than relying on
        // Dapper's automatic "IN @Ids" list expansion: Dapper detects when the underlying provider
        // natively supports array parameters (Npgsql does, Microsoft.Data.SqlClient - the real
        // neba-fwk provider - does not) and, when it does, binds the whole collection as a single
        // native array parameter instead of expanding the SQL text into "(@Ids1,@Ids2,...)". That
        // makes "IN @Ids" provider-dependent: correct against SQL Server, a syntax error against the
        // Postgres connection this test suite uses as a neba-fwk stand-in. Individually-numbered
        // scalar parameters sidestep that provider-detection branch entirely and work identically
        // against both.
        var idList = legacyHallOfFameIds.ToList();
        var parameters = new DynamicParameters();
        var placeholders = new string[idList.Count];
        for (var i = 0; i < idList.Count; i++)
        {
            var name = $"Id{i}";
            parameters.Add(name, idList[i]);
            placeholders[i] = "@" + name;
        }

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var rows = (await legacyConnection.QueryAsync<LegacyHallOfFameRow>(
            $"""
            SELECT
                Id,
                BowlerId,
                Category,
                Year
            FROM
                HallOfFame
            WHERE
                Id IN ({string.Join(",", placeholders)})
            """, parameters
        )).ToList();
#pragma warning restore DAP005

        var foundIds = rows.ConvertAll(r => r.Id).ToHashSet();
        foreach (var missingId in legacyHallOfFameIds.Except(foundIds))
        {
            logger.LogLegacyHallOfFameRowNotFound(missingId);
        }

        foreach (var row in rows)
        {
            await SyncRowAsync(row, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SyncRowAsync(LegacyHallOfFameRow row, CancellationToken ct)
    {
        var categories = MapCategory(row.Category, row.Id, logger);
        if (categories.Count == 0)
        {
            return;
        }

        var bowler = await db.Set<Bowler>().SingleOrDefaultAsync(b => b.LegacyId == row.BowlerId, ct);
        if (bowler is null)
        {
            logger.LogLegacyHallOfFameBowlerNotFound(row.Id, row.BowlerId);

            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: Hall of Fame induction",
                HtmlBody = new HallOfFameBowlerNotFoundEmail(row.Id, row.BowlerId, row.Year).ToHtmlBody()
            }, ct);

            return;
        }

        // Check locally-tracked entities before querying the database: within one batch, two rows
        // for the same bowler+year merge into the same induction, and the first row's Add hasn't
        // been persisted yet when the second row is processed, so a database-only query would miss
        // it and attempt to create (and then track) a second, colliding instance.
        var induction = db.HallOfFameInductions.Local.SingleOrDefault(i => i.Year == row.Year && i.BowlerId == bowler.Id)
            ?? await db.HallOfFameInductions.SingleOrDefaultAsync(i => i.Year == row.Year && i.BowlerId == bowler.Id, ct);

        if (induction is null)
        {
            induction = HallOfFameInduction.CreateFromLegacy(bowler.Id, row.Year, categories[0]);
            await db.HallOfFameInductions.AddAsync(induction, ct);

            foreach (var category in categories.Skip(1))
            {
                induction.ApplyLegacyCategory(category);
            }

            return;
        }

        var categoryCountBeforeApply = induction.Categories.Count;
        foreach (var category in categories)
        {
            induction.ApplyLegacyCategory(category);
        }

        if (induction.Categories.Count == categoryCountBeforeApply)
        {
            logger.LogLegacyHallOfFameAlreadySynced(row.Id, induction.Id);
        }
    }

    // Legacy Category is a plain int with no DB-level check constraint (Performance=100,
    // Service=200, Combined=300, Friend=500). Combined maps to both flags on the same website row -
    // exactly the bitmask scenario HallOfFameInduction.Categories was designed for. An unmapped value
    // is logged and contributes no category; callers treat an empty result as "skip this row" rather
    // than persisting an induction with zero categories.
    private static IReadOnlyList<HallOfFameCategory> MapCategory(int legacyCategory, int legacyHallOfFameId, ILogger<NewHallOfFameInductionSyncJob> logger)
    {
        switch (legacyCategory)
        {
            case 100:
                return [HallOfFameCategory.SuperiorPerformance];
            case 200:
                return [HallOfFameCategory.MeritoriousService];
            case 300:
                return [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.MeritoriousService];
            case 500:
                return [HallOfFameCategory.FriendOfNeba];
            default:
                logger.LogLegacyCategoryUnmapped(legacyHallOfFameId, legacyCategory);
                return [];
        }
    }
}

internal sealed record LegacyHallOfFameRow(int Id, int BowlerId, int Category, int Year);

internal static partial class NewHallOfFameInductionSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No Hall of Fame row found in neba-fwk for legacy id {LegacyHallOfFameId}; skipping.")]
    public static partial void LogLegacyHallOfFameRowNotFound(this ILogger<NewHallOfFameInductionSyncJob> logger, int legacyHallOfFameId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not map legacy category {LegacyCategory} for Hall of Fame row {LegacyHallOfFameId}; skipping row.")]
    public static partial void LogLegacyCategoryUnmapped(this ILogger<NewHallOfFameInductionSyncJob> logger, int legacyHallOfFameId, int legacyCategory);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No bowler found in website data for legacy bowler id {LegacyBowlerId} (Hall of Fame row {LegacyHallOfFameId}); manual intervention email sent.")]
    public static partial void LogLegacyHallOfFameBowlerNotFound(this ILogger<NewHallOfFameInductionSyncJob> logger, int legacyHallOfFameId, int legacyBowlerId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy Hall of Fame row {LegacyHallOfFameId} already reflected on induction {InductionId}; no new category to add.")]
    public static partial void LogLegacyHallOfFameAlreadySynced(this ILogger<NewHallOfFameInductionSyncJob> logger, int legacyHallOfFameId, HallOfFameId inductionId);
}

internal sealed class HallOfFameBowlerNotFoundEmail(int legacyHallOfFameId, int legacyBowlerId, int year)
{
    public string ToHtmlBody()
    {
        var body = $"""
                    <p>Hall of Fame induction with legacy id <strong>{WebUtility.HtmlEncode(legacyHallOfFameId.ToString(CultureInfo.CurrentCulture))}</strong> could not be synced and needs manual intervention.</p>
                    <p>Legacy bowler id: {WebUtility.HtmlEncode(legacyBowlerId.ToString(CultureInfo.CurrentCulture))}<br/>
                    Induction year: {WebUtility.HtmlEncode(year.ToString(CultureInfo.CurrentCulture))}</p>
                    <p>No bowler with this legacy id was found on the website. Sync the bowler first, then retry this induction manually.</p>
                    """;

        return EmailLayout.Wrap(body);
    }
}