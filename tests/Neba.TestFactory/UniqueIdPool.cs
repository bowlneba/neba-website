namespace Neba.TestFactory;

/// <summary>
/// Factory for creating <see cref="UniquePool{T}"/> instances.
/// </summary>
public static class UniquePool
{
    /// <summary>
    /// Creates a pool from <paramref name="values"/>, shuffled using the optional <paramref name="seed"/>.
    /// </summary>
    /// <param name="values">The unique values to pool.</param>
    /// <param name="seed">Optional seed for reproducible shuffling and null decisions.</param>
    /// <param name="probabilityOfValue">Probability (0.0–1.0) that <see cref="UniquePool{T}.GetNext"/> returns a value rather than null. Defaults to 1.0 (always a value).</param>
#pragma warning disable CA5394 // Random is acceptable here — used only for test data generation, not security
    public static UniquePool<T> Create<T>(IEnumerable<T> values, int? seed = null, float probabilityOfValue = 1.0f)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        List<T> shuffled = [.. values.OrderBy(_ => random.Next())];
        return new UniquePool<T>(shuffled, random, probabilityOfValue);
    }
#pragma warning restore CA5394
}

/// <summary>
/// Provides a pool of unique, pre-shuffled values for test data generation.
/// Ensures generated test data does not violate unique constraints.
/// Supports probabilistic null values for optional fields.
/// </summary>
/// <typeparam name="T">The type of values in the pool.</typeparam>
public sealed class UniquePool<T>
{
    private readonly List<T> _values;
    private readonly Random _random;
    private readonly float _probabilityOfValue;
    private int _currentIndex;

    internal UniquePool(List<T> values, Random random, float probabilityOfValue)
    {
        _values = values;
        _random = random;
        _probabilityOfValue = probabilityOfValue;
        _currentIndex = 0;
    }

    /// <summary>
    /// Returns the next unique value from the pool, or <c>null</c> (default) based on the configured probability.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the pool is exhausted and a value is needed.</exception>
#pragma warning disable CA1024, CA5394 // GetNext() appropriately named (mutates state); Random acceptable for test data
    public T? GetNext()
    {
        if (_random.NextDouble() >= _probabilityOfValue)
        {
            return default;
        }

        if (_currentIndex >= _values.Count)
        {
            throw new InvalidOperationException(
                $"UniquePool<{typeof(T).Name}> exhausted. Attempted to get value at index {_currentIndex} but pool only contains {_values.Count} values.");
        }

        return _values[_currentIndex++];
    }
#pragma warning restore CA1024, CA5394

    /// <summary>Gets the number of remaining values in the pool.</summary>
    public int RemainingCount => _values.Count - _currentIndex;
}