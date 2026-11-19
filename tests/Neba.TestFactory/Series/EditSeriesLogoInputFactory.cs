using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.EditSeriesLogo;

namespace Neba.TestFactory.Series;

public static class EditSeriesLogoInputFactory
{
    /// <summary>Sets the default logo. Pass <paramref name="removeLogo"/> to build a request that removes it.</summary>
    public static EditSeriesLogoInput Create(SeriesLogoInput? logo = null, bool removeLogo = false)
        => new() { Logo = removeLogo ? null : logo ?? SeriesLogoInputFactory.Create() };

    public static IReadOnlyCollection<EditSeriesLogoInput> Bogus(int count, int? seed = null)
        => [.. SeriesLogoInputFactory.Bogus(count, seed).Select(logo => Create(logo))];
}