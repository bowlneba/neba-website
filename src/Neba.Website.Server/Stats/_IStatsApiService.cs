namespace Neba.Website.Server.Stats;

internal interface IStatsApiService
{
    Task<StatsPageViewModel> GetStatsAsync(Ulid? seasonId = null, CancellationToken ct = default);
}
