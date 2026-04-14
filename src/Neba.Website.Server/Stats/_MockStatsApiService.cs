namespace Neba.Website.Server.Stats;

#pragma warning disable 

internal sealed class MockStatsApiService : IStatsApiService
{
    public Task<StatsPageViewModel> GetStatsAsync(Ulid? seasonId = null, CancellationToken ct = default)
        => Task.FromResult(MockStatsData.CurrentSeasonStats());
}
