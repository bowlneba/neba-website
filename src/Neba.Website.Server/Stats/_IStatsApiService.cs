namespace Neba.Website.Server.Stats;

internal interface IStatsApiService
{
    Task<StatsPageViewModel> GetStatsAsync(int? year = null, CancellationToken ct = default);
    Task<IndividualStatsPageViewModel?> GetIndividualStatsAsync(string bowlerId, int? year = null, CancellationToken ct = default);
}