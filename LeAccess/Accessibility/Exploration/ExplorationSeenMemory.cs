using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// Places the player has seen. Fixed-layout scenes keep theirs for the game session, so a
// discovered exit is still listed after a town trip; procedural scenes use their session as scope.
internal sealed class ExplorationSeenMemory
{
    private const float PositionCell = 2f;
    private readonly Dictionary<string, HashSet<string>> _scopes = new(StringComparer.Ordinal);
    private HashSet<string> _current;
    private string _scope;

    internal static string Scope(string sceneName, long session, bool fixedLayout) =>
        fixedLayout ? sceneName : $"{sceneName}#{session}";

    // Characters can walk about, so their name alone identifies them; places also keep a coarse position.
    internal static string Key(ExplorationCategory category, string name, Vector3 position) =>
        category == ExplorationCategory.Characters ? $"{(int)category}|{name}"
            : $"{(int)category}|{name}|{(int)MathF.Round(position.X / PositionCell)}|{(int)MathF.Round(position.Z / PositionCell)}";

    internal void Enter(string scope)
    {
        if (scope == _scope) return;
        _scope = scope;
        if (!_scopes.TryGetValue(scope, out _current)) _scopes[scope] = _current = new HashSet<string>(StringComparer.Ordinal);
    }

    internal bool Contains(string key) => _current?.Contains(key) == true;
    internal bool Add(string key)
    {
        bool added = _current?.Add(key) == true;
        if (added) Version++;
        return added;
    }

    // Bumped whenever a key is added, so persistence can tell when to save.
    internal int Version { get; private set; }

    internal IEnumerable<KeyValuePair<string, HashSet<string>>> FixedScopes()
    {
        foreach (var scope in _scopes)
            if (scope.Key.IndexOf('#') < 0) yield return scope;
    }

    internal int ImportFixed(string scope, IEnumerable<string> keys)
    {
        if (string.IsNullOrEmpty(scope) || scope.IndexOf('#') >= 0 || keys is null) return 0;
        if (!_scopes.TryGetValue(scope, out var set)) _scopes[scope] = set = new HashSet<string>(StringComparer.Ordinal);
        int added = 0;
        foreach (var key in keys)
            if (!string.IsNullOrEmpty(key) && set.Add(key)) added++;
        return added;
    }
    internal int Count => _current?.Count ?? 0;

    internal void Clear()
    {
        _scopes.Clear();
        _current = null;
        _scope = null;
        Version++;
    }
}
