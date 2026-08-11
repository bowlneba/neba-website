using System.Data;

using Dapper;

using ErrorOr;

using FluentValidation;

using Hangfire;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Identity;

namespace Neba.Api.Legacy.Bowlers;

internal static class UpdateBowler
{
    extension(Bowler bowler)
    {
        public ErrorOr<Updated> ApplyLegacyUpdate(
            string firstName,
            string lastName,
            string? middleName = null,
            NameSuffix? suffix = null,
            string? nickname = null,
            Gender? gender = null,
            DateOnly? dateOfBirth = null)
        {
            var name = Name.Create(firstName, lastName, middleName, suffix, nickname);
            if (name.IsError)
            {
                return name.Errors;
            }

            bowler.Name = name.Value;
            bowler.Gender = gender;
            bowler.DateOfBirth = dateOfBirth;

            return Result.Updated;
        }
    }
}

internal static class UpdateBowlerEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapUpdateBowler()
        {
            app.MapPost("/bowlers/update", (
                UpdateBowlerRequest request,
                IValidator<UpdateBowlerRequest> validator,
                IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }
                
                jobs.Enqueue<UpdateBowlerSyncJob>(job => job.SyncAsync(request.BowlerId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed record UpdateBowlerRequest(int BowlerId);

internal sealed class UpdateBowlerRequestValidator
    : AbstractValidator<UpdateBowlerRequest>
{
    public UpdateBowlerRequestValidator()
    {
        RuleFor(request => request.BowlerId)
            .GreaterThan(0);
    }
}

internal sealed class UpdateBowlerSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    ILogger<UpdateBowlerSyncJob> logger)
{
    public async Task SyncAsync(int legacyBowlerId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
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
            """,
            new
            {
                Id = legacyBowlerId
            });
#pragma warning restore DAP005

        if (row is null)
        {
            logger.LogLegacyBowlerNotFoundForUpdate(legacyBowlerId);

            return;
        }

        var gender = row.Gender switch
        {
            0 => Gender.Male,
            1 => Gender.Female,
            _ => null
        };
        
        var dateOfBirth = row.DateOfBirth.HasValue
            ? DateOnly.FromDateTime(row.DateOfBirth.Value)
            : (DateOnly?)null;

        var suffix = MapSuffix(row.Suffix, legacyBowlerId, logger);
        var (firstName, nickname) = row.FirstName.ExtractQuotedNickname();
        
        var existing = await db.Set<Bowler>().SingleOrDefaultAsync(bowler => bowler.LegacyId == legacyBowlerId, ct);

        if (existing is null)
        {
            // Decided: fall back to create rather than skip. A missing record here means either the
            // NewBowler call for this legacy id never landed, or this Update event arrived before it -
            // either way, the website should end up with a bowler record either way, not silently drop
            // the update because create-and-update happened to race.
            logger.LogLegacyBowlerNotSyncedYetForUpdate(legacyBowlerId);

            var created = Bowler.CreateFromLegacy(
                firstName,
                row.LastName,
                middleName: row.MiddleInitial,
                suffix: suffix,
                nickname: nickname,
                legacyId: row.Id,
                gender: gender,
                dateOfBirth: dateOfBirth);

            if (created.IsError)
            {
                logger.LogLegacyBowlerUpdateFailed(legacyBowlerId, string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            await db.Set<Bowler>().AddAsync(created.Value, ct);
            await db.SaveChangesAsync(ct);
            
            return;
        }
        
        var updated = existing.ApplyLegacyUpdate(
            firstName,
            row.LastName,
            middleName: row.MiddleInitial,
            suffix: suffix,
            nickname: nickname,
            gender: gender,
            dateOfBirth: dateOfBirth);

        if (updated.IsError)
        {
            logger.LogLegacyBowlerUpdateFailed(legacyBowlerId, string.Join("; ", updated.Errors.Select(error => error.Description)));

            return;
        }

        await db.SaveChangesAsync(ct);
    }
    
    // Identical to NewBowlerSyncJob.MapSuffix - see that file's comment for the full rationale.
    // Not yet extracted into the shared LegacyNameParsing.cs file since suffix mapping isn't part of
    // the nickname-parsing concern that file owns; if a third action needs it, that's the trigger to
    // extract a LegacySuffixParsing.cs alongside it rather than duplicating a third copy.
    private static NameSuffix? MapSuffix(string? legacySuffix, int legacyBowlerId, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(legacySuffix))
        {
            return null;
        }

        var normalized = legacySuffix.Trim().TrimEnd('.');

        var match = NameSuffix.List.SingleOrDefault(s =>
            string.Equals(s.Value.TrimEnd('.'), normalized, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            logger.LogLegacySuffixUnmappedForUpdate(legacyBowlerId, legacySuffix);
        }

        return match;
    }
}

internal static partial class UpdateBowlerSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No bowler found in neba-fwk for legacy id {LegacyBowlerId}; skipping update sync.")]
    public static partial void LogLegacyBowlerNotFoundForUpdate(this ILogger<UpdateBowlerSyncJob> logger, int legacyBowlerId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy bowler {LegacyBowlerId} has no existing website record; creating instead of updating.")]
    public static partial void LogLegacyBowlerNotSyncedYetForUpdate(this ILogger<UpdateBowlerSyncJob> logger, int legacyBowlerId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not map legacy suffix '{LegacySuffix}' (bowler {LegacyBowlerId}) to a known NameSuffix; leaving suffix blank.")]
    public static partial void LogLegacySuffixUnmappedForUpdate(this ILogger logger, int legacyBowlerId, string legacySuffix);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to apply legacy update for bowler {LegacyBowlerId}: {Errors}.")]
    public static partial void LogLegacyBowlerUpdateFailed(this ILogger<UpdateBowlerSyncJob> logger, int legacyBowlerId, string errors);
}