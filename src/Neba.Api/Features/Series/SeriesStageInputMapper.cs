using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series;

internal static class SeriesStageInputMapper
{
    public static StoredFile? ToLogo(SeriesLogoInput? logo)
        => logo is null
            ? null
            : new StoredFile { Container = logo.Container, Path = logo.Path, ContentType = logo.ContentType, SizeInBytes = logo.SizeInBytes };

    public static SeriesFinaleDefinition? ToFinale(SeriesFinaleInput? finale)
    {
        if (finale is null)
        {
            return null;
        }

        CertificationNumber? center = string.IsNullOrWhiteSpace(finale.BowlingCenterId)
            ? null
            : new CertificationNumber { Value = finale.BowlingCenterId };

        return new SeriesFinaleDefinition
        {
            FinalistCount = finale.FinalistCount,
            Date = finale.Date!.Value,
            StartTime = finale.StartTime!.Value,
            BowlingCenterId = center,
        };
    }

    public static IReadOnlyList<SeriesStageDefinition> ToDefinitions(IReadOnlyList<SeriesStageInput> stages)
        => [.. stages.Select(stage => new SeriesStageDefinition
        {
            StageId = stage.Id is null ? null : new SeriesStageId(stage.Id),
            Kind = StageKind.FromName(stage.Kind),
            TournamentId = new TournamentId(stage.TournamentId),
            AdvancingCount = stage.AdvancingCount,
            SquadDate = stage.SquadDate,
            SquadStartTime = stage.SquadStartTime,
        })];
}