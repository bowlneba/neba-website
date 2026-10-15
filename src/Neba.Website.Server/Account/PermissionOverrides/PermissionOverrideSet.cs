namespace Neba.Website.Server.Account.PermissionOverrides;

/// <summary>
/// The permission overrides on one user: values added regardless of role and values removed even
/// when a role grants them. Page-owned and mutable; the overrides section edits it in place.
/// </summary>
public sealed class PermissionOverrideSet
{
    private readonly HashSet<string> _added = new(StringComparer.Ordinal);
    private readonly HashSet<string> _removed = new(StringComparer.Ordinal);

    /// <summary>Creates an empty set (every permission at Default).</summary>
    public PermissionOverrideSet()
    {
    }

    /// <summary>Creates a set from stored added and removed permission values.</summary>
    public PermissionOverrideSet(IEnumerable<string> added, IEnumerable<string> removed)
    {
        ArgumentNullException.ThrowIfNull(added);
        ArgumentNullException.ThrowIfNull(removed);

        _added.UnionWith(added);
        _removed.UnionWith(removed);
    }

    /// <summary>Permission values allowed regardless of role.</summary>
    public IReadOnlyCollection<string> Added => _added;

    /// <summary>Permission values blocked regardless of role.</summary>
    public IReadOnlyCollection<string> Removed => _removed;

    /// <summary>The number of permissions that differ from Default.</summary>
    public int Count => _added.Count + _removed.Count;

    /// <summary>The state of one permission value.</summary>
    public PermissionOverrideState StateOf(string permissionValue)
    {
        if (_added.Contains(permissionValue))
        {
            return PermissionOverrideState.Added;
        }

        return _removed.Contains(permissionValue)
            ? PermissionOverrideState.Removed
            : PermissionOverrideState.Default;
    }

    /// <summary>Moves one permission value to <paramref name="state"/>.</summary>
    public void Set(string permissionValue, PermissionOverrideState state)
    {
        _added.Remove(permissionValue);
        _removed.Remove(permissionValue);

        if (state == PermissionOverrideState.Added)
        {
            _added.Add(permissionValue);
        }
        else if (state == PermissionOverrideState.Removed)
        {
            _removed.Add(permissionValue);
        }
    }

    /// <summary>Sets every permission back to Default.</summary>
    public void Clear()
    {
        _added.Clear();
        _removed.Clear();
    }

    /// <summary>Returns an independent copy.</summary>
    public PermissionOverrideSet Clone() => new(_added, _removed);
}