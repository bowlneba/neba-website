#pragma warning disable

using System.Diagnostics.CodeAnalysis;

using Neba.Application.Seasons;
using Neba.Domain.Seasons;

namespace Neba.Application.Tournaments;
    
[ExcludeFromCodeCoverage(Justification = "This is a temporary implementation until tournaments are in the database, at which point this will be deleted and the query handler will pull from the database instead of this hardcoded implementation.")]
internal class TournamentCount
    : ITournamentQueries
{
    public Task<int> GetTournamentCountForSeasonAsync(SeasonDto season, CancellationToken cancellationToken)
    {
        var tournaments = season.EndDate.Year switch
        {
            2019 => 20,
            2021 => 21,
            2022 => 19,
            2023 => 17,
            2024 => 19,
            2025 => 17,
            _ => throw new ArgumentOutOfRangeException(nameof(season), "Season not supported")
        };

        return Task.FromResult(tournaments);
    }
}