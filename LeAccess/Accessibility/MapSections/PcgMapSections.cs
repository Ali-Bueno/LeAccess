using System;
using System.Globalization;
using System.Collections.Generic;
using LE.PCG;
using LE.PCG.Data.Tiles;
using LE.PCG.Data.Rooms;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.MapSections;

internal readonly record struct PcgSectionSnapshot(long Session, string SectionId, int Number,
    bool IsEntrance, bool IsExit, int? ConnectedPassages, string KindKey);

// Generated room rectangles describe allocation, not necessarily walkable floor. Only
// classify full Floor cells with authoritative corner elevations, leaving edges unknown.
internal static class PcgMapSections
{
    private static IntPtr _levelPointer, _dataPointer;
    private static IntPtr _roomsPointer, _tilemapPointer, _generatorPointer;
    private static uint _seed;
    private static long _playerSession, _session;
    private static string _lastFailure;

    internal static bool TryCapture(MapSectionWorldSnapshot world, out PcgSectionSnapshot snapshot)
    {
        snapshot = default;
        try
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || (int)scene.handle != world.SceneHandle ||
                scene.name != world.SceneName || scene.path != world.ScenePath ||
                !LevelUtility.TryGetLevel(scene, out var level) || !IsAlive(level) || !level.isActiveAndEnabled ||
                !level.IsLevelGenerationCompleted || (int)level.RootScene.handle != world.SceneHandle)
            { Invalidate(); return false; }
            var generator = level.GeneratorData;
            var data = generator?.Level;
            if (generator is null || generator.WasCollected || data is null || data.WasCollected ||
                data.Rooms is null || data.Rooms.WasCollected || data.Tilemap is null || data.Tilemap.WasCollected ||
                data.Tileset is null || data.Tileset.WasCollected)
            { Invalidate(); return false; }
            if (_levelPointer != level.Pointer || _dataPointer != data.Pointer || _seed != data.Seed ||
                _roomsPointer != data.Rooms.Pointer || _tilemapPointer != data.Tilemap.Pointer ||
                _generatorPointer != generator.Pointer ||
                _playerSession != world.Session)
            {
                _levelPointer = level.Pointer; _dataPointer = data.Pointer; _seed = data.Seed;
                _roomsPointer = data.Rooms.Pointer; _tilemapPointer = data.Tilemap.Pointer;
                _generatorPointer = generator.Pointer;
                _playerSession = world.Session; _session++;
            }
            var scale = data.Tileset.TileSize;
            if (!Positive(scale.x) || !Positive(scale.y) || !Positive(scale.z)) return false;
            var matrix = level.LevelToWorld;
            if (!FiniteMatrix(matrix) || !float.IsFinite(matrix.determinant) || matrix.determinant == 0) return false;
            var inverse = matrix.inverse;
            if (!FiniteMatrix(inverse)) return false;
            var local = inverse.MultiplyPoint3x4(new Vector3((float)world.X, (float)world.Y, (float)world.Z));
            double x = local.x / scale.x, y = local.y / scale.y, z = local.z / scale.z;
            if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z) ||
                x < int.MinValue || x > int.MaxValue || z < int.MinValue || z > int.MaxValue) return false;
            var cell = new int2((int)Math.Floor(x), (int)Math.Floor(z));
            var tilemap = data.Tilemap;
            if (!tilemap.InBounds(cell)) return false;
            var hash = tilemap.GetTileHash(cell);
            if (hash.BottomLeft != TileKey.Floor || hash.BottomRight != TileKey.Floor ||
                hash.TopRight != TileKey.Floor || hash.TopLeft != TileKey.Floor) return false;
            var offsets = BakedTilemap.hashPosLookup;
            var heights = tilemap.Heights;
            if (offsets is null || offsets.WasCollected || offsets.Length != 4 ||
                heights is null || heights.WasCollected || tilemap.Size.x <= 0 || tilemap.Size.y <= 0) return false;
            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            for (int corner = 0; corner < 4; corner++)
            {
                long cx = (long)cell.x + offsets[corner].x, cz = (long)cell.y + offsets[corner].y;
                long index = cx + cz * tilemap.Size.x;
                if (cx < 0 || cz < 0 || cx >= tilemap.Size.x || cz >= tilemap.Size.y || index >= heights.Length) return false;
                float height = heights[(int)index];
                if (!float.IsFinite(height)) return false;
                min = Math.Min(min, height); max = Math.Max(max, height);
            }
            // Body radius is supplied by the actual offline actor. No guessed online radius.
            double inverseYScale = Math.Sqrt((double)inverse[1] * inverse[1] + (double)inverse[5] * inverse[5] +
                (double)inverse[9] * inverse[9]);
            double margin = MapPlayerFloor.HeightAllowance(world) * inverseYScale / scale.y;
            if (y < min - margin || y > max + margin) return false;
            int match = -1;
            for (int index = 0; index < data.Rooms.Length; index++)
            {
                var room = data.Rooms[index];
                if (room is null || room.WasCollected) { Invalidate(); return false; }
                var rect = room.Rect;
                if (x < rect.min.x || z < rect.min.y || x >= (double)rect.min.x + rect.size.x ||
                    z >= (double)rect.min.y + rect.size.y) continue;
                if (match >= 0) return false; // Ambiguous allocation must not select an arbitrary room.
                match = index;
            }
            if (match < 0) return false;
            var metadata = data.Rooms[match].GrammarMetadata;
            if (metadata is not null && metadata.WasCollected) return false;
            if (!TryGetKind(data.Rooms[match], level.Archetype, out string kind)) return false;
            snapshot = new PcgSectionSnapshot(_session, match.ToString(CultureInfo.InvariantCulture), match + 1,
                metadata?.IsEntrance ?? false, metadata?.IsExit ?? false, CountLayoutConnections(data.Rooms[match]), kind);
            return true;
        }
        catch (Exception ex)
        {
            Invalidate();
            string failure = ex.GetType().Name + ": " + ex.Message;
            if (_lastFailure != failure) { _lastFailure = failure; Plugin.Log?.LogWarning("PCG sections: " + failure); }
            return false;
        }
    }

    private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    private static bool TryGetKind(BakedRoom room, LevelArchetype archetype, out string kind)
    {
        kind = archetype switch
        { LevelArchetype.Cave => "map.section.cave_room", LevelArchetype.Tomb => "map.section.tomb_room", _ => "map.section.room" };
        var template = room.Template;
        if (template is null) return true; // Geometry still identifies a room; no hallway claim without metadata.
        if (template.WasCollected) return false;
        int hallwayTag = LE.PCG.Data.Constants.ROOM_TAG_HALLWAY;
        if (hallwayTag < 0 || hallwayTag >= RoomTagMask.TAG_COUNT) return false;
        // Native JoinDoorsWithRoom marks the retained template with ROOM_TAG_HALLWAY.
        // This describes the current floor-backed allocation, not nearby door positions.
        if (template.TagMask.HasTag(hallwayTag)) kind = "map.section.passage";
        return true;
    }
    private static int? CountLayoutConnections(BakedRoom room)
    {
        var doors = room.Doors;
        if (doors is null || doors.WasCollected) return null;
        var indices = new HashSet<int>();
        var positions = new HashSet<(int, int)>();
        int count = 0;
        foreach (var door in doors)
        {
            if (door.Index < 0 || door.Size <= 0 || !indices.Add(door.Index) ||
                !positions.Add((door.Position.x, door.Position.y))) return null;
            if (door.Occupied) count++;
        }
        // Native layout commands set both attached doors' Occupied flags and undo
        // them on rollback. This describes design connections, not open passage state.
        return count;
    }
    private static bool FiniteMatrix(Matrix4x4 matrix)
    { for (int i = 0; i < 16; i++) if (!float.IsFinite(matrix[i])) return false; return true; }
    private static void Invalidate()
    { _levelPointer = _dataPointer = _roomsPointer = _tilemapPointer = _generatorPointer = IntPtr.Zero; _playerSession = 0; }
    internal static void Reset() { Invalidate(); _lastFailure = null; MapPlayerFloor.Reset(); }
}
