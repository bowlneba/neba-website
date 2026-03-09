using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Neba.Infrastructure.Database.Converters;

internal sealed class UlidTypedIdConverter<TId>(ConverterMappingHints? mappingHints)
    : ValueConverter<TId, string>(BuildToProvider(), BuildFromProvider(), mappingHints)
    where TId : struct
{
    public UlidTypedIdConverter() : this((ConverterMappingHints?)null) { }

    private static Expression<Func<TId, string>> BuildToProvider()
    {
        const string valuePropertyName = "Value";

        var valueProperty = typeof(TId).GetProperty(valuePropertyName)
            ?? throw new InvalidOperationException($"Type {typeof(TId)} must expose a '{valuePropertyName}' property.");

        var param = Expression.Parameter(typeof(TId), "id");
        var value = Expression.Property(param, valueProperty);

        Expression toProvider = valueProperty.PropertyType switch
        {
            { } type when type == typeof(Ulid) => Expression.Call(value, nameof(Ulid.ToString), Type.EmptyTypes),
            { } type when type == typeof(string) => value,
            _ => throw new InvalidOperationException($"Type {typeof(TId)} must expose a '{valuePropertyName}' property of type {nameof(Ulid)} or {nameof(String)}."),
        };

        return Expression.Lambda<Func<TId, string>>(toProvider, param);
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