using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LeAccess.Accessibility.Exploration;

// The one exploration file every character shares, and the merge of the older per-character files into it.
internal static class ExplorationShared
{
    internal const string SharedName = "shared";
    internal const string DeathsSuffix = ".deaths";
    private const string FileExtension = ".bin";
    private const string DeathPrefix = "death|";

    internal static string SharedPath(string folder) => Path.Combine(folder, SharedName + FileExtension);

    internal static string DeathsPath(string folder, string key) => Path.Combine(folder, key + DeathsSuffix + FileExtension);

    internal static string LegacyPath(string folder, string key) => Path.Combine(folder, key + FileExtension);

    // The last death place is the one seen key that is a character's own, not map knowledge.
    internal static bool IsDeath(string key) => key != null && key.StartsWith(DeathPrefix, StringComparison.Ordinal);

    internal static bool TryReadFile(string path, out ExplorationSnapshot snapshot, out string reason)
    {
        using var stream = File.OpenRead(path);
        return ExplorationStore.TryRead(stream, out snapshot, out reason);
    }

    // Union of source into target: ground deduplicated by explored-map cell, keys by value, both capped per scope.
    internal static void Merge(ExplorationSnapshot target, ExplorationSnapshot source)
    {
        foreach (var scope in source.Explored)
        {
            if (!target.Explored.TryGetValue(scope.Key, out var points))
            {
                if (target.Explored.Count >= ExplorationStore.MaxScopes) continue;
                points = new List<System.Numerics.Vector3>();
            }
            var map = new ExploredMap();
            map.Import(points);
            map.Import(scope.Value);
            target.Explored[scope.Key] = map.Points.ToList();
        }
        foreach (var scope in source.Seen)
        {
            if (!target.Seen.TryGetValue(scope.Key, out var keys))
            {
                if (target.Seen.Count >= ExplorationStore.MaxScopes) continue;
                keys = new List<string>();
            }
            var known = new HashSet<string>(keys, StringComparer.Ordinal);
            foreach (string key in scope.Value)
                if (!IsDeath(key) && keys.Count < ExplorationStore.MaxKeysPerScope && known.Add(key)) keys.Add(key);
            target.Seen[scope.Key] = keys;
        }
    }

    // Only the death keys of a snapshot, grouped by scope.
    internal static ExplorationSnapshot DeathsOf(ExplorationSnapshot source)
    {
        var deaths = new ExplorationSnapshot();
        foreach (var scope in source.Seen)
        {
            var keys = scope.Value.Where(IsDeath).ToList();
            if (keys.Count > 0) deaths.Seen[scope.Key] = keys;
        }
        return deaths;
    }

    // Every readable per-character file in the folder merged; an unreadable one is reported and skipped.
    internal static ExplorationSnapshot MergeLegacy(string folder, Action<string, string> onCorrupt, out int files)
    {
        var merged = new ExplorationSnapshot();
        files = 0;
        if (!Directory.Exists(folder)) return merged;
        foreach (string path in Directory.GetFiles(folder, "*" + FileExtension).OrderBy(p => p, StringComparer.Ordinal))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name == SharedName || name.EndsWith(DeathsSuffix, StringComparison.Ordinal)) continue;
            try
            {
                if (!TryReadFile(path, out var snapshot, out string reason)) { onCorrupt(path, reason); continue; }
                Merge(merged, snapshot);
                files++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
        return merged;
    }
}
