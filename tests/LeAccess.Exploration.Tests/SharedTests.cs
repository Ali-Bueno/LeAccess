using System.Numerics;
using LeAccess.Accessibility.Exploration;
using static Program;

internal static class SharedTests
{
    internal static void Run()
    {
        MergeUnion();
        MergeDedupes();
        MergeCaps();
        Deaths();
        LegacyFolder();
        SwitchKeepsMemory();
    }

    private static ExplorationSnapshot Character(Vector3[] points, params string[] keys)
    {
        var snapshot = new ExplorationSnapshot();
        snapshot.Explored["Town|0"] = points.ToList();
        snapshot.Seen["Town"] = keys.ToList();
        return snapshot;
    }

    private static bool RoundTrips(ExplorationSnapshot snapshot)
    {
        var stream = new MemoryStream();
        ExplorationStore.Write(stream, snapshot);
        return ExplorationStore.TryRead(new MemoryStream(stream.ToArray()), out _, out _);
    }

    private static void Write(string path, ExplorationSnapshot snapshot)
    {
        using var stream = File.Create(path);
        ExplorationStore.Write(stream, snapshot);
    }

    private static void MergeUnion()
    {
        var merged = new ExplorationSnapshot();
        ExplorationShared.Merge(merged, Character(new Vector3[] { new(1, 0, 1), new(3, 0, 1) }, "1|Salida|5|1", "3|Maeve"));
        var second = Character(new Vector3[] { new(3, 0, 1), new(9, 0, 9) }, "3|Maeve", "1|Pozo|0|0");
        second.Explored["Ruins|"] = new List<Vector3> { new(5, 0, 5) };
        ExplorationShared.Merge(merged, second);
        Check(merged.Explored["Town|0"].Count == 3, "points of two characters are united without the shared one twice");
        Check(merged.Explored["Ruins|"].Count == 1, "a scope only the second explored is added");
        Check(merged.Seen["Town"].Count == 3 && merged.Seen["Town"].Distinct().Count() == 3, "seen keys are united");
    }

    private static void MergeDedupes()
    {
        var merged = new ExplorationSnapshot();
        var near = Character(new Vector3[] { new(1, 0, 1), new(1.2f, 0.3f, 1.1f), new(1, 0, 1) });
        ExplorationShared.Merge(merged, near);
        ExplorationShared.Merge(merged, near);
        Check(merged.Explored["Town|0"].Count == 1, "points in the same explored-map cell collapse to one");
        Check(merged.Explored["Town|0"][0] == new Vector3(1, 0, 1), "the first character's point is kept");
        Check(merged.Seen["Town"].Count == 0 && RoundTrips(merged), "an empty seen scope survives a round trip");
    }

    private static void MergeCaps()
    {
        var merged = new ExplorationSnapshot();
        var a = new ExplorationSnapshot();
        var b = new ExplorationSnapshot();
        int half = ExplorationStore.MaxPointsPerScope / 2;
        a.Explored["Big|0"] = Enumerable.Range(0, ExplorationStore.MaxPointsPerScope).Select(i => new Vector3(i * 2 + 1, 0, 1)).ToList();
        b.Explored["Big|0"] = Enumerable.Range(half, ExplorationStore.MaxPointsPerScope).Select(i => new Vector3(i * 2 + 1, 0, 1)).ToList();
        a.Seen["Big"] = Enumerable.Range(0, ExplorationStore.MaxKeysPerScope).Select(i => $"1|k{i}|0|0").ToList();
        b.Seen["Big"] = Enumerable.Range(ExplorationStore.MaxKeysPerScope / 2, ExplorationStore.MaxKeysPerScope).Select(i => $"1|k{i}|0|0").ToList();
        for (int i = 0; i < ExplorationStore.MaxScopes + 5; i++) b.Explored[$"S{i}|"] = new List<Vector3> { new(1, 0, 1) };
        ExplorationShared.Merge(merged, a);
        ExplorationShared.Merge(merged, b);
        Check(merged.Explored["Big|0"].Count == ExplorationStore.MaxPointsPerScope, "united points stay within the per-scope cap");
        Check(merged.Seen["Big"].Count == ExplorationStore.MaxKeysPerScope, "united keys stay within the per-scope cap");
        Check(merged.Explored.Count == ExplorationStore.MaxScopes, "scopes stay within the scope cap");
        Check(RoundTrips(merged), "a capped merge still writes a readable file");
    }

    private static void Deaths()
    {
        var merged = new ExplorationSnapshot();
        var snapshot = Character(new Vector3[] { new(1, 0, 1) }, "death|1.0|0.0|2.0", "room|a|1", "1|Salida|5|1");
        ExplorationShared.Merge(merged, snapshot);
        Check(merged.Seen["Town"].SequenceEqual(new[] { "room|a|1", "1|Salida|5|1" }), "death places are not merged into the shared file");
        var own = ExplorationShared.DeathsOf(snapshot);
        Check(own.Seen["Town"].SequenceEqual(new[] { "death|1.0|0.0|2.0" }) && own.Explored.Count == 0, "death places are split out per character");
    }

    private static void LegacyFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "leaccess-shared-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            Write(Path.Combine(folder, "offline-ana-1.bin"), Character(new Vector3[] { new(1, 0, 1), new(3, 0, 1) }, "3|Maeve", "death|1.0|0.0|2.0"));
            Write(Path.Combine(folder, "online-ben-2.bin"), Character(new Vector3[] { new(3, 0, 1), new(9, 0, 9) }, "1|Pozo|0|0"));
            File.WriteAllBytes(Path.Combine(folder, "offline-bad-3.bin"), new byte[] { 1, 2, 3 });
            Write(Path.Combine(folder, "offline-ana-1.deaths.bin"), new ExplorationSnapshot());
            var bad = new List<string>();
            var merged = ExplorationShared.MergeLegacy(folder, (path, _) => bad.Add(Path.GetFileName(path)), out int files);
            Check(files == 2 && bad.SequenceEqual(new[] { "offline-bad-3.bin" }), "a corrupt old file is reported and skipped");
            Check(merged.Explored["Town|0"].Count == 3, "online and offline characters share one ground");
            Check(merged.Seen["Town"].OrderBy(k => k).SequenceEqual(new[] { "1|Pozo|0|0", "3|Maeve" }), "places of both are merged, deaths left out");
            Check(File.Exists(Path.Combine(folder, "offline-ana-1.bin")) && File.Exists(Path.Combine(folder, "online-ben-2.bin")),
                "old files stay in place");
            Check(ExplorationShared.MergeLegacy(Path.Combine(folder, "none"), (_, _) => { }, out int none).Explored.Count == 0 && none == 0,
                "a missing folder merges nothing");
            Write(ExplorationShared.SharedPath(folder), merged);
            Check(ExplorationShared.TryReadFile(ExplorationShared.SharedPath(folder), out var back, out _) && back.Explored["Town|0"].Count == 3,
                "the shared file reads back");
            Check(ExplorationShared.MergeLegacy(folder, (_, _) => { }, out int again).Explored["Town|0"].Count == 3 && again == 2,
                "the shared and deaths files are not taken for characters");
            Check(ExplorationShared.TryReadFile(ExplorationShared.LegacyPath(folder, "offline-ana-1"), out var ana, out _) &&
                  ExplorationShared.DeathsOf(ana).Seen["Town"].Count == 1, "a character's old file still yields its death place");
        }
        finally { Directory.Delete(folder, true); }
    }

    // ExplorationPersistence.Switch keeps the memories and drops only the previous character's death places.
    private static void SwitchKeepsMemory()
    {
        var ground = new ExploredMemory(() => new ExploredMap());
        ground.ImportFixed("Town|0", new[] { new Vector3(1, 0, 1) });
        var seen = new ExplorationSeenMemory();
        seen.ImportFixed("Town", new[] { "3|Maeve", "room|a|1", "death|1.0|0.0|2.0" });
        foreach (var scope in seen.FixedScopes()) scope.Value.RemoveWhere(ExplorationShared.IsDeath);
        Check(ground.FixedScopes.Sum(s => s.Value.CellCount) == 1, "a switch keeps the shared ground");
        Check(seen.FixedScopes().Single().Value.SetEquals(new[] { "3|Maeve", "room|a|1" }), "a switch keeps the shared places and drops the death place");
    }
}
