using System.Collections.Concurrent;
using System.Reflection;

using ErrorOr;

namespace Neba.Infrastructure.Caching;

/// <summary>
/// Provides reflection-based utilities for working with ErrorOr types in caching scenarios.
/// Enables unwrapping ErrorOr values before caching and rewrapping them after retrieval.
/// </summary>
internal static class ErrorOrCacheHelper
{
    private static readonly ConcurrentDictionary<Type, ErrorOrTypeInfo> TypeInfoCache = new();

    /// <summary>
    /// Determines whether the specified type is ErrorOr&lt;T&gt;.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>true if the type is ErrorOr&lt;T&gt;; otherwise, false.</returns>
    public static bool IsErrorOrType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(ErrorOr<>);
    }

    /// <summary>
    /// Extracts the inner type T from ErrorOr&lt;T&gt;.
    /// </summary>
    /// <param name="errorOrType">The ErrorOr&lt;T&gt; type.</param>
    /// <returns>The inner type T.</returns>
    /// <exception cref="ArgumentException">Thrown when the type is not ErrorOr&lt;T&gt;.</exception>
    public static Type GetInnerType(Type errorOrType)
    {
        ArgumentNullException.ThrowIfNull(errorOrType);

        return !IsErrorOrType(errorOrType)
            ? throw new ArgumentException($"Type {errorOrType.Name} is not ErrorOr<T>", nameof(errorOrType))
            : errorOrType.GetGenericArguments()[0];
    }

    /// <summary>
    /// Checks whether an ErrorOr instance contains an error.
    /// </summary>
    /// <param name="errorOrInstance">The ErrorOr instance to check.</param>
    /// <returns>true if the instance contains an error; otherwise, false.</returns>
    public static bool IsError(IErrorOr errorOrInstance)
    {
        ArgumentNullException.ThrowIfNull(errorOrInstance);

        return errorOrInstance.IsError;
    }

    /// <summary>
    /// Extracts the value from a successful ErrorOr instance.
    /// </summary>
    /// <param name="errorOrInstance">The ErrorOr instance.</param>
    /// <returns>The unwrapped value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when trying to get value from an error state.</exception>
    public static object? GetValue(object errorOrInstance)
    {
        ArgumentNullException.ThrowIfNull(errorOrInstance);

        var typeInfo = GetTypeInfo(errorOrInstance.GetType());
        return typeInfo.ValueProperty.GetValue(errorOrInstance);
    }

    /// <summary>
    /// Wraps a value of type T into ErrorOr&lt;T&gt;.
    /// </summary>
    /// <param name="innerType">The inner type T.</param>
    /// <param name="value">The value to wrap.</param>
    /// <returns>An ErrorOr&lt;T&gt; instance containing the value.</returns>
    public static object WrapValue(Type innerType, object value)
    {
        ArgumentNullException.ThrowIfNull(innerType);
        ArgumentNullException.ThrowIfNull(value);

        var errorOrType = typeof(ErrorOr<>).MakeGenericType(innerType);
        var typeInfo = GetTypeInfo(errorOrType);

        // Use the implicit operator to wrap the value into ErrorOr<T>
        return typeInfo.ImplicitOperator.Invoke(null, [value])!;
    }

    private static ErrorOrTypeInfo GetTypeInfo(Type errorOrType)
        => TypeInfoCache.GetOrAdd(errorOrType, BuildTypeInfo);

    private static ErrorOrTypeInfo BuildTypeInfo(Type errorOrType)
    {
        var innerType = errorOrType.GetGenericArguments()[0];

        var valueProperty = errorOrType.GetProperty(
            nameof(ErrorOr<object>.Value),
            BindingFlags.Public | BindingFlags.Instance);

        // Find the implicit conversion operator from T to ErrorOr<T>
        var implicitOperator = errorOrType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m =>
                m.Name == "op_Implicit" &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == innerType);

        if (valueProperty is null)
        {
            throw new InvalidOperationException($"Type {errorOrType.Name} does not have a Value property.");
        }

        if (implicitOperator is null)
        {
            throw new InvalidOperationException($"Type {errorOrType.Name} does not have an implicit conversion operator from {innerType.Name}.");
        }

        return new ErrorOrTypeInfo
        {
            InnerType = innerType,
            ValueProperty = valueProperty,
            ImplicitOperator = implicitOperator
        };

    }

    private sealed class ErrorOrTypeInfo
    {
        public required Type InnerType { get; init; }
        public required PropertyInfo ValueProperty { get; init; }
        public required MethodInfo ImplicitOperator { get; init; }
    }
}