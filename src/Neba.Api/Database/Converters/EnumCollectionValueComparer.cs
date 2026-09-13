using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Neba.Api.Database.Converters;

/// <summary>
/// Builds a <see cref="ValueComparer{T}"/> for an <see cref="IReadOnlyCollection{T}"/> property
/// that's converted to a scalar column (e.g. a delimited string), so EF Core compares elements
/// rather than falling back to reference equality on the collection instance.
/// </summary>
internal static class EnumCollectionValueComparer
{
    public static ValueComparer<IReadOnlyCollection<T>> Create<T>() =>
        new(
            (left, right) => (left ?? Array.Empty<T>()).SequenceEqual(right ?? Array.Empty<T>()),
            collection => (collection ?? Array.Empty<T>()).Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            collection => (collection ?? Array.Empty<T>()).ToList());
}