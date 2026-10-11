using System.Numerics;
using LeAccess.Accessibility.Exploration;
using static Program;

internal static class TownTests
{
    private const int WalkableArea = 0, BlockedArea = 1;

    internal static void Run()
    {
        SeededTownRoutes();
        SeedIsBounded();
        OffMeshGoal();
    }

    // A 20 x 10 floor made of two triangles, with a wall of unwalkable-area triangles across x 10..12 except a gap.
    private static NavMeshSeed Town(out FakeGrid grid)
    {
        grid = new FakeGrid(Enumerable.Range(0, 10).Select(z => new string(Enumerable.Range(0, 20)
            .Select(x => x >= 10 && x < 12 && z < 6 ? '#' : '.').ToArray())).ToArray());
        var vertices = new[]
        {
            new Vector3(0, 0, 0), new Vector3(20, 0, 0), new Vector3(20, 0, 10), new Vector3(0, 0, 10),
            new Vector3(10, 0, 0), new Vector3(12, 0, 0), new Vector3(12, 0, 6), new Vector3(10, 0, 6)
        };
        var indices = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 };
        var seed = new NavMeshSeed();
        seed.Begin(vertices, indices, new[] { WalkableArea, WalkableArea, BlockedArea, BlockedArea }, 1 << WalkableArea);
        return seed;
    }

    private static void SeededTownRoutes()
    {
        var seed = Town(out var grid);
        var map = new ExploredMap();
        int added = seed.Step(map);
        Check(seed.Done && added == map.CellCount && added > 0, "a small town is seeded in one step");
        Check(map.Explored(new Vector3(1, 0, 1)) && map.Explored(new Vector3(19, 0, 9)), "the whole floor is explored");
        Check(seed.Step(map) == 0, "seeding twice adds nothing");
        // The wall triangles are covered by the floor triangles too; the grid, not the seed, decides what is clear.
        var from = new Vector3(1.5f, 0, 1.5f);
        var target = new Vector3(17.5f, 0, 1.5f);
        Check(map.TryRoute(from, target, 1.5f, grid, out var path, out _), $"an unvisited town routes ({map.LastFailure})");
        bool clear = true;
        for (int index = 1; index < path.Count; index++) clear &= grid.Clear(path[index - 1], path[index]);
        Check(clear && path.Any(point => point.Z > 6), "the route detours through the gap");
    }

    private static void SeedIsBounded()
    {
        int triangles = NavMeshSeed.TrianglesPerStep * 3;
        var vertices = new[] { new Vector3(0, 0, 0), new Vector3(2, 0, 0), new Vector3(0, 0, 2) };
        var indices = Enumerable.Range(0, triangles).SelectMany(_ => new[] { 0, 1, 2 }).ToArray();
        var seed = new NavMeshSeed();
        seed.Begin(vertices, indices, Enumerable.Repeat(WalkableArea, triangles).ToArray(), 1);
        var map = new ExploredMap();
        seed.Step(map);
        Check(!seed.Done, "a step takes at most its triangle budget");
        seed.Step(map);
        seed.Step(map);
        Check(seed.Done, "later steps finish the mesh");

        var limited = new NavMeshSeed();
        limited.Begin(new[] { new Vector3(0, 0, 0), new Vector3(1000, 0, 0), new Vector3(0, 0, 1000) }, new[] { 0, 1, 2 },
            new[] { WalkableArea }, 1);
        var capped = new ExploredMap();
        limited.Step(capped);
        Check(capped.CellCount == ExploredMap.MaxCells, "seeding respects MaxCells");
        Check(limited.Done, "a full map ends seeding");
        var broken = new NavMeshSeed();
        broken.Begin(new[] { new Vector3(float.NaN, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1) }, new[] { 0, 1, 2, 0, 1, 9 },
            new[] { WalkableArea, WalkableArea }, 1);
        Check(broken.Step(new ExploredMap()) == 0, "non-finite and out-of-range triangles are skipped");
    }

    // A stash on a counter stands off the mesh: the route ends at the explored cell within its range.
    private static void OffMeshGoal()
    {
        var grid = new FakeGrid(Enumerable.Range(0, 10).Select(z => new string(Enumerable.Range(0, 20)
            .Select(x => z < 3 && x >= 8 && x < 14 ? ' ' : '.').ToArray())).ToArray());
        var map = new ExploredMap();
        map.Import(Enumerable.Range(0, 10).Select(x => new Vector3(x * 2 + 1, 0, 7)));
        var from = new Vector3(1, 0, 7);
        var stash = new Vector3(11, 0, 1);
        Check(map.TryRoute(from, stash, 6, grid, out _, out _), $"an off-mesh target within its range routes ({map.LastFailure})");
        Check(map.TryRoute(from, stash, 0.5f, grid, out _, out _) == false, "an off-mesh target beyond its range has no route");
        Check(!grid.TrySample(stash, out _), "the stash stands off the mesh");
        map.Import(new[] { new Vector3(11, 0, 5), new Vector3(11, 0, 3) });
        Check(map.TryRoute(from, stash, 0.5f, grid, out var path, out _) && path[^1] == stash,
            $"a cell up to a cell width away serves a tiny interaction range ({map.LastFailure})");
    }
}
