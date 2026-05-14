using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Neba.Domain.HallOfFame;
using Neba.Domain.Tournaments;

namespace Neba.Infrastructure.Database.Converters;

internal sealed class TournamentRoundValueConverter
    : ValueConverter<IReadOnlyCollection<TournamentRound>, int>
{
    public TournamentRoundValueConverter()
        : base(
            categories => categories.Aggregate(0, (value, category) => value | category.Value),
            value => TournamentRound.FromValue(value).ToArray().AsReadOnly())
    { }
}