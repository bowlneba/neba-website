using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Neba.Infrastructure.Database.Converters;

internal sealed class UlidTypedIdConverter<TId>(ConverterMappingHints? mappingHints = null)
    : ValueConverter<TId, string>(BuildToProvider(), BuildFromProvider(), mappingHints)
    where TId : struct
{
    private static Expression<Func<TId, string>> BuildToProvider()
    {
        var param = Expression.Parameter(typeof(TId), "id");
        var toString = Expression.Call(param, nameof(ToString), typeArguments: null);

        return Expression.Lambda<Func<TId, string>>(toString, param);
    }

    private static Expression<Func<string, TId>> BuildFromProvider()
    {
        var param = Expression.Parameter(typeof(string), "value");
        var ctor = typeof(TId).GetConstructor([typeof(string)])
            ?? throw new InvalidOperationException($"Type {typeof(TId)} must have a constructor that accepts a single string parameter.");

        return Expression.Lambda<Func<string, TId>>(
            Expression.New(ctor, param),
            param);
    }
}