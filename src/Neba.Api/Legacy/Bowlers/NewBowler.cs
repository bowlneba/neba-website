using System.Data;

using Dapper;

using ErrorOr;

using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Legacy.Bowlers;

internal static class NewBowlerEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapNewBowler()
        {
            app.MapPost("/bowlers/new", (
                NewBowlerRequest request,
                [FromServices] IValidator<NewBowlerRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<NewBowlerSyncJob>(job => job.SyncAsync(request.BowlerId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed record NewBowlerRequest(int BowlerId);

internal sealed class NewBowlerRequestValidator
    : AbstractValidator<NewBowlerRequest>
{
    public NewBowlerRequestValidator()
    {
        RuleFor(x => x.BowlerId)
            .GreaterThan(0);
    }
}

internal sealed class NewBowlerSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    ILogger<NewBowlerSyncJob> logger)
{
    public async Task SyncAsync(int legacyBowlerId, CancellationToken ct)
    {
        using var _ = LegacyActor.EnterActorScope();

        // Dapper.AOT is not enabled project-wide (this is the codebase's first Dapper usage) - the
        // interceptor-based source generator DAP005 nudges toward opting in, but plain Dapper reflection
        // is fine for this single, low-traffic legacy query, so it's suppressed here rather than pulling
        // in the AOT package for one call site.
#pragma warning disable DAP005
        var row = await legacyConnection.QuerySingleOrDefaultAsync<LegacyBowlerRow>(
            """
            SELECT
                Id,
                FirstName,
                MiddleInitial,
                LastName,
                Suffix,
                Gender,
                DateOfBirth
            FROM
                Bowlers
            WHERE
                Id = @Id
            """, new
            {
                Id = legacyBowlerId
            }
        );
#pragma warning restore DAP005

        if (row is null)
        {
            logger.LogLegacyBowlerNotFound(legacyBowlerId);
            return;
        }

        var gender = LegacyBowlerMapping.MapGender(row.Gender);
        var dateOfBirth = LegacyBowlerMapping.MapDateOfBirth(row.DateOfBirth);

        var existing = await db.Set<Bowler>().SingleOrDefaultAsync(b => b.LegacyId == legacyBowlerId, ct);
        if (existing is not null)
        {
            // Decided: strictly create-only, not an upsert. A second call for the same LegacyId
            // (Hangfire's automatic retry, or an accidental double-trigger from the Software side)
            // is assumed to be a duplicate of a sync that already succeeded, and is a pure no-op —
            // it does not update the existing bowler's fields. This keeps the job simple (no
            // update method needed on Bowler for this action) and matches the idempotency
            // requirement in the Testing section below without introducing "which fields are
            // safe to overwrite from a legacy row" as a question this action has to answer.
            logger.LogLegacyBowlerAlreadySynced(legacyBowlerId, existing.Id);
            return;
        }

        var suffix = LegacyBowlerMapping.MapSuffix(row.Suffix);
        if (suffix is null && !string.IsNullOrWhiteSpace(row.Suffix))
        {
            logger.LogLegacySuffixUnmapped(legacyBowlerId, row.Suffix);
        }

        var bowler = Bowler.CreateFromLegacy(
            row.FirstName,
            row.LastName,
            middleName: row.MiddleInitial,
            suffix: suffix,
            legacyId: row.Id,
            gender: gender,
            dateOfBirth: dateOfBirth);

        if (bowler.IsError)
        {
            logger.LogLegacyBowlerCreateFailed(legacyBowlerId, string.Join("; ", bowler.Errors.Select(e => e.Description)));
            return;
        }

        await db.Set<Bowler>().AddAsync(bowler.Value, ct);
        await db.SaveChangesAsync(ct);
    }
}

internal sealed record LegacyBowlerRow(int Id, string FirstName, string? MiddleInitial, string LastName, string? Suffix, int Gender, DateTime? DateOfBirth);

internal static class LegacyBowlerExtensions
{
    extension(Bowler)
    {
        public static ErrorOr<Bowler> CreateFromLegacy(
            string firstName,
            string lastName,
            string? middleName = null,
            NameSuffix? suffix = null,
            string? nickname = null,
            int? legacyId = null,
            Gender? gender = null,
            DateOnly? dateOfBirth = null)
        {
            var name = Name.Create(firstName, lastName, middleName, suffix, nickname);

            return name.IsError
                ? name.Errors
                : new Bowler
                {
                    Id = BowlerId.New(),
                    Name = name.Value,
                    LegacyId = legacyId,
                    Gender = gender,
                    DateOfBirth = dateOfBirth
                };
        }
    }
}

internal static partial class NewBowlerSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No bowler found in neba-fwk for legacy id {LegacyBowlerId}; skipping sync.")]
    public static partial void LogLegacyBowlerNotFound(this ILogger<NewBowlerSyncJob> logger, int legacyBowlerId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy bowler {LegacyBowlerId} already synced as {BowlerId}; treating as a duplicate call and skipping.")]
    public static partial void LogLegacyBowlerAlreadySynced(this ILogger<NewBowlerSyncJob> logger, int legacyBowlerId, BowlerId bowlerId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not map legacy suffix '{LegacySuffix}' (bowler {LegacyBowlerId}) to a known NameSuffix; leaving suffix blank.")]
    public static partial void LogLegacySuffixUnmapped(this ILogger logger, int legacyBowlerId, string legacySuffix);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to create bowler from legacy id {LegacyBowlerId}: {Errors}.")]
    public static partial void LogLegacyBowlerCreateFailed(this ILogger<NewBowlerSyncJob> logger, int legacyBowlerId, string errors);
}