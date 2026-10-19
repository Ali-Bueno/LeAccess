using System.Numerics;
using LeAccess.Accessibility.Exploration;

// One-unit tiles standing in for the NavMesh. Ground: '.' floor at height 0, '#' wall, 'D' door
// (floor while open), 'L' a link the player walks and paths cross but a raycast cannot (the seam between two
// NavMesh surfaces). Upper layer: '=' deck at UpperHeight, '1'-'9' ramp steps of RampStep.
internal sealed class FakeGrid : IWalkability
{
    internal const float Climb = 0.6f;
    internal const float SampleRadius = 1.5f;
    internal const float FloorTolerance = 1f;
    internal const float UpperHeight = 5f;
    internal const float RampStep = 0.5f;
    private const float Step = 0.05f;
    private const float Margin = 0.05f;
    private readonly string[] _ground, _upper;

    internal FakeGrid(string[] ground, string[] upper = null)
    {
        _ground = ground;
        _upper = upper;
    }

    internal bool DoorOpen { get; set; }
    internal int Samples { get; private set; }
    internal int Clears { get; private set; }
    internal int Paths { get; private set; }

    internal void ResetCounters() => Samples = Clears = Paths = 0;

    public bool TrySample(Vector3 point, out Vector3 onMesh) => TrySampleWithin(point, SampleRadius, out onMesh);

    public bool TrySampleWithin(Vector3 point, float radius, out Vector3 onMesh)
    {
        Samples++;
        onMesh = default;
        float best = radius;
        bool found = false;
        for (int z = Tile(point.Z - radius); z <= Tile(point.Z + radius); z++)
            for (int x = Tile(point.X - radius); x <= Tile(point.X + radius); x++)
                foreach (float height in Surfaces(x, z))
                {
                    var candidate = new Vector3(Math.Clamp(point.X, x + Margin, x + 1 - Margin), height,
                        Math.Clamp(point.Z, z + Margin, z + 1 - Margin));
                    float distance = Vector3.Distance(candidate, point);
                    if (distance > best) continue;
                    best = distance;
                    onMesh = candidate;
                    found = true;
                }
        return found;
    }

    // Walks the surface like NavMesh.Raycast; ending on another floor than the target is not clear.
    public bool Clear(Vector3 from, Vector3 to)
    {
        Clears++;
        int x = Tile(from.X), z = Tile(from.Z);
        float? height = Continue(x, z, from.Y, FloorTolerance);
        if (height is null || Link(x, z)) return false;
        float dx = to.X - from.X, dz = to.Z - from.Z;
        int steps = Math.Max(1, (int)MathF.Ceiling(MathF.Sqrt(dx * dx + dz * dz) / Step));
        for (int step = 1; step <= steps; step++)
        {
            float t = (float)step / steps;
            int nx = Tile(from.X + dx * t), nz = Tile(from.Z + dz * t);
            if (nx == x && nz == z) continue;
            if (nx != x && nz != z && Continue(nx, z, height.Value, Climb) is null &&
                Continue(x, nz, height.Value, Climb) is null) return false;
            height = Continue(nx, nz, height.Value, Climb);
            if (height is null || Link(nx, nz)) return false;
            x = nx;
            z = nz;
        }
        return MathF.Abs(height.Value - to.Y) <= FloorTolerance;
    }

    // A breadth-first tile walk standing in for NavMesh.CalculatePath; unlike Clear it crosses links.
    public bool TryPath(Vector3 from, Vector3 to, List<Vector3> corners)
    {
        Paths++;
        corners.Clear();
        int fx = Tile(from.X), fz = Tile(from.Z), tx = Tile(to.X), tz = Tile(to.Z);
        if (Continue(fx, fz, from.Y, FloorTolerance) is not { } height) return false;
        var start = (fx, fz, height);
        var parents = new Dictionary<(int X, int Z, float Y), (int X, int Z, float Y)> { [start] = start };
        var queue = new Queue<(int X, int Z, float Y)>();
        queue.Enqueue(start);
        while (queue.TryDequeue(out var node))
        {
            if (node.X == tx && node.Z == tz && MathF.Abs(node.Y - to.Y) <= FloorTolerance)
            {
                var tiles = new List<Vector3>();
                for (var at = node; at != start; at = parents[at]) tiles.Add(new Vector3(at.X + 0.5f, at.Y, at.Z + 0.5f));
                tiles.Reverse();
                corners.Add(from);
                if (tiles.Count > 1) corners.AddRange(tiles.GetRange(0, tiles.Count - 1));
                corners.Add(to);
                return true;
            }
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    if (dx != 0 && dz != 0 && Continue(node.X + dx, node.Z, node.Y, Climb) is null &&
                        Continue(node.X, node.Z + dz, node.Y, Climb) is null) continue;
                    if (Continue(node.X + dx, node.Z + dz, node.Y, Climb) is not { } next) continue;
                    var step = (node.X + dx, node.Z + dz, next);
                    if (parents.TryAdd(step, node)) queue.Enqueue(step);
                }
        }
        return false;
    }

    private bool Link(int x, int z) => At(_ground, x, z) == 'L';

    private float? Continue(int x, int z, float height, float tolerance)
    {
        float? best = null;
        foreach (float surface in Surfaces(x, z))
            if (MathF.Abs(surface - height) <= tolerance && (best is null || MathF.Abs(surface - height) < MathF.Abs(best.Value - height)))
                best = surface;
        return best;
    }

    private IEnumerable<float> Surfaces(int x, int z)
    {
        char ground = At(_ground, x, z);
        if (ground is '.' or 'L' || ground == 'D' && DoorOpen) yield return 0;
        char upper = At(_upper, x, z);
        if (upper == '=') yield return UpperHeight;
        else if (upper is >= '1' and <= '9') yield return (upper - '0') * RampStep;
    }

    private static char At(string[] rows, int x, int z) =>
        rows is null || z < 0 || z >= rows.Length || x < 0 || x >= rows[z].Length ? ' ' : rows[z][x];

    private static int Tile(float value) => (int)MathF.Floor(value);
}
