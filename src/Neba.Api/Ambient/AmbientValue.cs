namespace Neba.Api.Ambient;

/// <summary>
/// AsyncLocal-backed ambient value with scope-restore-on-dispose semantics, shared by
/// <see cref="Neba.Api.Identity.AmbientActorContext"/> and
/// <see cref="Neba.Api.Auditing.AmbientCorrelationContext"/> so each owns only the bit of API
/// that's actually unique to it — a field name, and (for the correlation id) its own
/// <c>Capture</c> logic.
/// </summary>
internal sealed class AmbientValue<T>
{
    private readonly AsyncLocal<T?> _current = new();

    public T? Current => _current.Value;

    /// <summary>
    /// Sets the ambient value for the remainder of the current async call chain. Dispose the
    /// returned scope when the value no longer applies (e.g. at the end of a background job).
    /// </summary>
    public IDisposable Set(T value)
    {
        var previous = _current.Value;
        _current.Value = value;
        return new Scope(this, previous);
    }

    private sealed class Scope(AmbientValue<T> owner, T? previous) : IDisposable
    {
        public void Dispose() => owner._current.Value = previous;
    }
}
