using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.Website.Server.Series;

internal sealed record SeriesDetailViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string SeasonId { get; init; }
    public required string SeasonDescription { get; init; }
    public required string SeasonDateRange { get; init; }
    public required bool IsLocked { get; init; }
    public SeriesFinaleViewModel? Finale { get; init; }
    public Uri? LogoUrl { get; init; }

    /// <summary>The Series has a Finale exactly when it has one (the Razor reads this, not a null check).</summary>
    public bool HasFinale => Finale is not null;

    /// <summary>The stored logo, resent unchanged by the edit page.</summary>
    public SeriesLogoInput? Logo { get; init; }
    public required IReadOnlyList<SeriesStageViewModel> Stages { get; init; }
}

internal sealed record SeriesFinaleViewModel
{
    public required int FinalistCount { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }
    public string? BowlingCenterId { get; init; }

    /// <summary>"Callahan's Bowl-O-Rama, Seekonk, MA", or null while the host center is to be announced.</summary>
    public string? Host { get; init; }
}