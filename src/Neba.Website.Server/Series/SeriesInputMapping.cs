using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.EditSeries;

namespace Neba.Website.Server.Series;

internal static class SeriesInputMapping
{
    public static SeriesInput ToCreateInput(SeriesFormModel model)
        => new() { Name = model.Name.Trim(), SeasonId = model.SeasonId!, Logo = model.Logo, Stages = ToStageInputs(model), Finale = ToFinaleInput(model.Finale) };

    public static EditSeriesInput ToEditInput(SeriesFormModel model)
        => new() { Name = model.Name.Trim(), Logo = model.Logo, Stages = ToStageInputs(model), Finale = ToFinaleInput(model.Finale) };

    private static SeriesFinaleInput? ToFinaleInput(SeriesFinaleFormModel? finale)
    {
        if (finale is null)
        {
            return null;
        }

        var centerId = string.IsNullOrWhiteSpace(finale.BowlingCenterId) ? null : finale.BowlingCenterId;

        return new SeriesFinaleInput
        {
            FinalistCount = finale.FinalistCount ?? 0, // an empty count reaches the server as 0, which it rejects by name
            Date = finale.Date,
            StartTime = finale.StartTime,
            BowlingCenterId = centerId,
        };
    }

    private static IReadOnlyList<SeriesStageInput> ToStageInputs(SeriesFormModel model)
        => [.. model.Stages.Select(row => new SeriesStageInput
        {
            Id = row.StageId,
            Kind = row.Kind,
            TournamentId = row.TournamentId ?? string.Empty,
            AdvancingCount = row.Kind == StageKinds.Placement ? null : row.AdvancingCount,
            SquadDate = row.Kind == StageKinds.Survivor ? row.SquadDate : null,
            SquadStartTime = row.Kind == StageKinds.Survivor ? row.SquadStartTime : null,
        })];

    public static SeriesFormModel FromDetail(SeriesDetailViewModel detail)
    {
        var model = new SeriesFormModel
        {
            Name = detail.Name,
            SeasonId = detail.SeasonId,
            Logo = detail.Logo,
            LogoUrl = detail.LogoUrl,
            Finale = detail.Finale is { } finale
                ? new SeriesFinaleFormModel
                {
                    FinalistCount = finale.FinalistCount,
                    Date = finale.Date,
                    StartTime = finale.StartTime,
                    BowlingCenterId = finale.BowlingCenterId,
                }
                : null,
        };
        model.Stages.AddRange(detail.Stages.Select(stage => new SeriesStageRowModel
        {
            StageId = stage.Id,
            Kind = stage.Kind,
            TournamentId = stage.TournamentId,
            AdvancingCount = stage.AdvancingCount,
            SquadDate = stage.SquadDate,
            SquadStartTime = stage.SquadStartTime,
        }));
        return model;
    }

    /// <summary>Routes <c>Series.Stages[i]</c> errors onto rows and <c>Series.Finale.*</c> errors onto the Finale section; returns the rest for the banner.</summary>
    public static IReadOnlyList<string> ApplyFieldErrors(SeriesFormModel model, IReadOnlyList<Services.ApiFieldError> errors)
    {
        foreach (var row in model.Stages)
        {
            row.Errors.Clear();
        }

        model.Finale?.Errors.Clear();

        var general = new List<string>();

        foreach (var error in errors)
        {
            if (TryRowIndex(error.Name, out var index) && index < model.Stages.Count)
            {
                model.Stages[index].Errors.Add(error.Reason);
            }
            else if (model.Finale is not null && error.Name.StartsWith("Series.Finale", StringComparison.OrdinalIgnoreCase))
            {
                model.Finale.Errors.Add(error.Reason);
            }
            else
            {
                general.Add(error.Reason);
            }
        }

        return general;
    }

    // "Series.Stages[3]" or "Series.Stages[3].AdvancingCount" (validator) -> 3
    private static bool TryRowIndex(string name, out int index)
    {
        index = -1;
        const string prefix = "Series.Stages[";
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var close = name.IndexOf(']', prefix.Length);
        return close > prefix.Length
            && int.TryParse(name.AsSpan(prefix.Length, close - prefix.Length), out index);
    }
}