using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Neba.Api.Features.HallOfFame.Domain;

namespace Neba.Api.Database.Converters;

internal sealed class HallOfFameCategoryValueConverter
    : ValueConverter<IReadOnlyCollection<HallOfFameCategory>, int>
{
    public HallOfFameCategoryValueConverter()
        : base(
            categories => categories.Aggregate(0, (value, category) => value | category.Value),
            value => HallOfFameCategory.FromValue(value).ToArray().AsReadOnly())
    { }
}