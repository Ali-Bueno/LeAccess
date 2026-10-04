using System.Numerics;
using System.Text;
using LeAccess.Accessibility.Exploration;
using static Program;

internal static class StoreTests
{
    internal static void Run()
    {
        RoundTrip();
        Rejects();
        Import();
        ImportedRouting();
    }

    private static byte[] Save(ExplorationSnapshot snapshot)
    {
        var stream = new MemoryStream();
        ExplorationStore.Write(stream, snapshot);
        return stream.ToArray();
    }

    private static bool Read(byte[] bytes, out ExplorationSnapshot snapshot, out string reason) =>
        ExplorationStore.TryRead(new MemoryStream(bytes), out snapshot, out reason);

    private static ExplorationSnapshot Sample()
    {
        var snapshot = new ExplorationSnapshot();
        snapshot.Explored["Town|0"] = new List<Vector3> { new(1, 0, 3), new(3.5f, -2, 5), new(-7, 4.25f, 1e5f) };
        snapshot.Explored["Ruins|"] = new List<Vector3>();
        snapshot.Seen["Town"] = new List<string> { "1|Salida|5|1", "3|Maeve", "2|Pozo de ñandú ☃|0|0" };
        return snapshot;
    }

    private static void RoundTrip()
    {
        var bytes = Save(Sample());
        Check(Read(bytes, out var back, out _), "a saved snapshot reads back");
        Check(back.Explored.Count == 2 && back.Seen.Count == 1, "scope counts survive");
        Check(back.Explored["Town|0"].SequenceEqual(Sample().Explored["Town|0"]), "points survive exactly");
        Check(back.Explored["Ruins|"].Count == 0, "an empty scope survives");
        Check(back.Seen["Town"].SequenceEqual(Sample().Seen["Town"]), "keys survive, Unicode included");
        Check(Read(Save(new ExplorationSnapshot()), out var empty, out _) && empty.Explored.Count == 0, "an empty snapshot round-trips");
    }

    private static void Rejects()
    {
        var good = Save(Sample());
        for (int length = 0; length < good.Length; length++)
            Check(!Read(good[..length], out _, out var why) && why != null, $"truncation at {length} rejected");
        Check(!Read(good.Concat(new byte[] { 0 }).ToArray(), out _, out _), "trailing data rejected");
        var badMagic = (byte[])good.Clone();
        badMagic[0] ^= 0xFF;
        Check(!Read(badMagic, out _, out var magic) && magic == "bad magic", "bad magic rejected");
        var badVersion = (byte[])good.Clone();
        badVersion[4] = 99;
        Check(!Read(badVersion, out _, out var version) && version == "unsupported version", "bad version rejected");

        var nan = Sample();
        nan.Explored["Town|0"][1] = new Vector3(float.NaN, 0, 0);
        Check(!Read(Save(nan), out _, out var nanWhy) && nanWhy == "non-finite point", "NaN point rejected");
        nan.Explored["Town|0"][1] = new Vector3(0, float.PositiveInfinity, 0);
        Check(!Read(Save(nan), out _, out _), "infinite point rejected");

        Check(!Read(Craft(writer => writer.Write(ExplorationStore.MaxScopes + 1)), out _, out _), "too many scopes rejected");
        Check(!Read(Craft(writer =>
        {
            writer.Write(1);
            Text(writer, "Town|0");
            writer.Write(ExplorationStore.MaxPointsPerScope + 1);
        }), out _, out _), "oversized point count rejected");
        Check(!Read(Craft(writer =>
        {
            writer.Write(1);
            Text(writer, "Town|0");
            writer.Write(int.MaxValue);
        }), out _, out _), "huge point count rejected without allocating");
        Check(!Read(Craft(writer => writer.Write(-1)), out _, out _), "negative count rejected");
        Check(!Read(Craft(writer =>
        {
            writer.Write(0);
            writer.Write(1);
            Text(writer, "Town");
            writer.Write(ExplorationStore.MaxKeysPerScope + 1);
        }), out _, out _), "oversized key count rejected");
        Check(!Read(Craft(writer =>
        {
            writer.Write(2);
            for (int index = 0; index < 2; index++) { Text(writer, "Town|0"); writer.Write(0); }
        }), out _, out _), "duplicate scope rejected");
        Check(!Read(Array.Empty<byte>(), out _, out _), "empty file rejected");

        var random = new Random(7);
        for (int round = 0; round < 500; round++)
        {
            var fuzzed = (byte[])good.Clone();
            for (int flips = 0; flips < 3; flips++) fuzzed[random.Next(fuzzed.Length)] = (byte)random.Next(256);
            Read(fuzzed, out _, out _);
        }
        Check(true, "random corruption never throws");
    }

    private static byte[] Craft(Action<BinaryWriter> body)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, new UTF8Encoding(false), leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("LEXP"));
            writer.Write(ExplorationStore.Version);
            body(writer);
        }
        return stream.ToArray();
    }

    private static void Text(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);
    }

    private static void Import()
    {
        var grid = new FakeGrid(Rows(20, 10, (_, _) => '.'));
        var map = new ExploredMap();
        var points = new List<Vector3>
        {
            new(1, 0, 1), new(1.2f, 0.3f, 1.1f), new(1, 5, 1), new(float.NaN, 0, 0), new(3, 0, 1), new(1, 0, 1)
        };
        int added = map.Import(points);
        Check(added == 3 && map.CellCount == 3, $"import dedupes by column and floor, skips non-finite ({added})");
        Check(map.Explored(new Vector3(3, 0, 1)) && map.Explored(new Vector3(1, 5, 1)), "imported cells are explored");
        Check(map.Points.Count == 3, "points are exported");
        Check(map.Import(points) == 0, "importing again adds nothing");
        Check(map.EdgeChecks == 0 && grid.Samples == 0 && grid.Clears == 0, "import makes no walkability calls");
        Check(map.Import(null) == 0, "null import is harmless");

        var big = new ExploredMap();
        int imported = big.Import(Enumerable.Range(0, ExploredMap.MaxCells + 10).Select(i => new Vector3(i * 2 + 1, 0, 1)));
        Check(imported == ExploredMap.MaxCells && big.CellCount == ExploredMap.MaxCells, "import stops at MaxCells");
    }

    // A wall splits the room except for a gap; imported cells exist on both sides of the wall.
    private static void ImportedRouting()
    {
        var grid = new FakeGrid(Rows(20, 10, (x, z) => x == 10 && z < 6 ? '#' : '.'));
        var cells = new List<Vector3>();
        for (int z = 0; z < 5; z++)
            for (int x = 0; x < 10; x++) cells.Add(new Vector3(x * 2 + 1, 0, z * 2 + 1));
        var map = new ExploredMap();
        map.Import(cells);
        grid.ResetCounters();
        var from = new Vector3(1.5f, 0, 1.5f);
        var target = new Vector3(17.5f, 0, 1.5f);
        Check(map.TryRoute(from, target, 1.5f, grid, out var path, out _), $"imported cells route ({map.LastFailure})");
        Check(map.EdgeChecks > 0 && map.EdgeChecks <= map.MaxExpansions * 8, "links were proven lazily during the search");
        bool clear = true;
        for (int index = 1; index < path.Count; index++) clear &= grid.Clear(path[index - 1], path[index]);
        Check(clear, "the route avoids the wall the imported cells do not know about");
        Check(path.Any(point => point.Z > 6), "the route detours through the gap");
        long proven = map.EdgeChecks;
        Check(map.TryRoute(from, target, 1.5f, grid, out _, out _) && map.EdgeChecks == proven, "proven links are cached");
    }

    private static string[] Rows(int width, int depth, Func<int, int, char> tile) =>
        Enumerable.Range(0, depth).Select(z => new string(Enumerable.Range(0, width).Select(x => tile(x, z)).ToArray())).ToArray();
}
