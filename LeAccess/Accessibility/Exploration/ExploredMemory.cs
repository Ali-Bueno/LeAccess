using System;
using System.Collections.Generic;

namespace LeAccess.Accessibility.Exploration;

// Explored ground per scene. Fixed layouts keep theirs for the game session, so returning to a zone
// keeps what was explored; a procedural scene starts over whenever its session changes.
internal sealed class ExploredMemory
{
    private readonly Func<ExploredMap> _create;
    private readonly Dictionary<string, ExploredMap> _fixed = new(StringComparer.Ordinal);
    private ExploredMap _procedural;
    private string _proceduralScope;

    internal ExploredMemory(Func<ExploredMap> create) =>
        _create = create ?? throw new ArgumentNullException(nameof(create));

    internal int FixedCount => _fixed.Count;

    // A scene variant can change a fixed layout, so it is part of the scope.
    internal static string Scope(string sceneName, int? variant, long session, bool fixedLayout) =>
        fixedLayout ? $"{sceneName}|{variant}" : $"{sceneName}#{session}";

    internal ExploredMap Resolve(string sceneName, int? variant, long session, bool fixedLayout)
    {
        string scope = Scope(sceneName, variant, session, fixedLayout);
        if (fixedLayout)
        {
            if (!_fixed.TryGetValue(scope, out var map)) _fixed[scope] = map = _create();
            return map;
        }
        if (_procedural is null || _proceduralScope != scope)
        {
            _procedural = _create();
            _proceduralScope = scope;
        }
        return _procedural;
    }

    internal IEnumerable<KeyValuePair<string, ExploredMap>> FixedScopes => _fixed;

    // Saved fixed-layout ground; returns the cells added.
    internal int ImportFixed(string scope, IEnumerable<System.Numerics.Vector3> points)
    {
        if (string.IsNullOrEmpty(scope)) return 0;
        if (!_fixed.TryGetValue(scope, out var map)) _fixed[scope] = map = _create();
        return map.Import(points);
    }

    internal void Clear()
    {
        _fixed.Clear();
        _procedural = null;
        _proceduralScope = null;
    }
}
