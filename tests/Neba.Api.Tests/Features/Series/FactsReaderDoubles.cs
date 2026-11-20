using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>Strict <see cref="ISeriesAdvancementFactsReader"/> mocks that return the facts a test holds.</summary>
internal static class FactsReaderDoubles
{
    /// <summary>Returns the facts the function produces at the time of each call, so a test can change them between runs.</summary>
    public static Mock<ISeriesAdvancementFactsReader> Reading(Func<AdvancementFacts> facts)
    {
        var reader = new Mock<ISeriesAdvancementFactsReader>(MockBehavior.Strict);
        reader.Setup(r => r.ReadAsync(It.IsAny<SeriesAggregate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(facts);

        return reader;
    }

    /// <summary>Returns the facts the function builds for the Series being read. The function may throw to model a failed read.</summary>
    public static Mock<ISeriesAdvancementFactsReader> ReadingFor(Func<SeriesAggregate, AdvancementFacts> facts)
    {
        var reader = new Mock<ISeriesAdvancementFactsReader>(MockBehavior.Strict);
        reader.Setup(r => r.ReadAsync(It.IsAny<SeriesAggregate>(), It.IsAny<CancellationToken>()))
            .Returns((SeriesAggregate series, CancellationToken _) => Task.FromResult(facts(series)));

        return reader;
    }

    /// <summary>Has no setup, so any read fails the test.</summary>
    public static Mock<ISeriesAdvancementFactsReader> Untouched()
        => new(MockBehavior.Strict);

    public static void VerifyReadTimes(Mock<ISeriesAdvancementFactsReader> reader, Func<Times> times)
        => reader.Verify(r => r.ReadAsync(It.IsAny<SeriesAggregate>(), It.IsAny<CancellationToken>()), times);
}