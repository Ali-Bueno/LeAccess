using System.Numerics;
using LeAccess.Accessibility.Exploration;

// One-unit tiles standing in for the NavMesh. Ground: '.' floor at height 0, '#' wall, 'D' door
// (floor while open). Upper layer: '=' deck at UpperHeight, '1'-'9' ramp steps of RampStep.
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

    internal void ResetCounters() => Samples = Clears = 0;

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
        if (height is null) return false;
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
            if (height is null) return false;
            x = nx;
            z = nz;
        }
        return MathF.Abs(height.Value - to.Y) <= FloorTolerance;
    }

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
        if (ground == '.' || ground == 'D' && DoorOpen) yield return 0;
        char upper = At(_upper, x, z);
        if (upper == '=') yield return UpperHeight;
        else if (upper is >= '1' and <= '9') yield return (upper - '0') * RampStep;
    }

    private static char At(string[] rows, int x, int z) =>
        rows is null || z < 0 || z >= rows.Length || x < 0 || x >= rows[z].Length ? ' ' : rows[z][x];

    private static int Tile(float value) => (int)MathF.Floor(value);
}
