using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EHG.Multiplayer;
using LeAccess.Accessibility.MapSections;

namespace LeAccess.Accessibility.Exploration;

// Saves each character's explored ground and seen places (fixed-layout scenes only) between game runs.
internal static class ExplorationPersistence
{
    private const double SaveSeconds = 30;
    private const int MaxNameLength = 64;
    private static string _key;
    private static long _session;
    private static int _savedCells, _savedSeenVersion;
    private static double _nextSave;
    private static string _lastWarning;

    internal static void Update(double now, MapSectionWorldSnapshot world)
    {
        try
        {
            string key = CurrentKey();
            if (key == null) return;
            if (key != _key)
            {
                Flush();
                Switch(key);
                _session = world.Session;
                _nextSave = now + SaveSeconds;
            }
            else if (world.Session != _session)
            {
                Flush();
                _session = world.Session;
            }
            else if (now >= _nextSave && IsDirty())
            {
                _nextSave = now + SaveSeconds;
                Save();
            }
        }
        catch (Exception ex) { Warn($"update failed: {ex.Message}"); }
    }

    internal static void Flush()
    {
        try { if (_key != null && IsDirty()) Save(); }
        catch (Exception ex) { Warn($"save failed: {ex.Message}"); }
    }

    internal static void Cleanup()
    {
        Flush();
        _key = null;
        _session = 0;
        _savedCells = _savedSeenVersion = 0;
        _nextSave = 0;
        _lastWarning = null;
    }

    private static string CurrentKey()
    {
        var character = PlayerFinder.getPlayerData();
        if (character is null || character.WasCollected) return null;
        string name = character.CharacterName;
        if (string.IsNullOrWhiteSpace(name)) return null;
        return $"{(GameplayEnvironment.IsOnlineClient ? "online" : "offline")}-{SanitizeName(name)}-{character.CharacterClass}";
    }

    internal static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var text = new StringBuilder();
        foreach (char c in name.Trim())
            text.Append(char.IsControl(c) || Array.IndexOf(invalid, c) >= 0 || c == ' ' ? '_' : c);
        return text.Length > MaxNameLength ? text.ToString(0, MaxNameLength) : text.ToString();
    }

    private static string PathFor(string key) =>
        Path.Combine(BepInEx.Paths.ConfigPath, "LeAccess", "Exploration", key + ".bin");

    private static bool IsDirty() => Cells() != _savedCells || ExplorationProbe.SeenMemory.Version != _savedSeenVersion;

    private static int Cells() =>
        ExplorationBeacon.ExploredGround.FixedScopes.Sum(scope => scope.Value.CellCount);

    private static void Switch(string key)
    {
        ExplorationBeacon.ExploredGround.Clear();
        ExplorationProbe.SeenMemory.Clear();
        ExplorationBeacon.ForgetMap();
        _key = key;
        Load(key);
        _savedCells = Cells();
        _savedSeenVersion = ExplorationProbe.SeenMemory.Version;
    }

    private static void Load(string key)
    {
        string path = PathFor(key);
        try
        {
            if (!File.Exists(path)) return;
            ExplorationSnapshot snapshot;
            string reason;
            long size;
            using (var stream = File.OpenRead(path))
            {
                size = stream.Length;
                if (!ExplorationStore.TryRead(stream, out snapshot, out reason))
                {
                    stream.Close();
                    Quarantine(path, reason);
                    return;
                }
            }
            int cells = 0, places = 0;
            foreach (var scope in snapshot.Explored) cells += ExplorationBeacon.ExploredGround.ImportFixed(scope.Key, scope.Value);
            foreach (var scope in snapshot.Seen) places += ExplorationProbe.SeenMemory.ImportFixed(scope.Key, scope.Value);
            Plugin.Log.LogInfo($"[Exploration saved] loaded key={key} scopes={snapshot.Explored.Count}+{snapshot.Seen.Count} cells={cells} places={places} bytes={size}");
        }
        catch (Exception ex) { Warn($"load failed for {key}: {ex.Message}"); }
    }

    private static void Quarantine(string path, string reason)
    {
        Warn($"corrupt exploration file ({reason}); set aside as {Path.GetFileName(path)}.corrupt");
        try { File.Move(path, path + ".corrupt", overwrite: true); }
        catch (Exception ex) { Warn($"could not set the corrupt file aside: {ex.Message}"); }
    }

    private static void Save()
    {
        var snapshot = new ExplorationSnapshot();
        foreach (var scope in ExplorationBeacon.ExploredGround.FixedScopes)
            if (scope.Value.CellCount > 0) snapshot.Explored[scope.Key] = scope.Value.Points.ToList();
        foreach (var scope in ExplorationProbe.SeenMemory.FixedScopes())
            if (scope.Value.Count > 0) snapshot.Seen[scope.Key] = scope.Value.ToList();
        string path = PathFor(_key), temp = path + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        long size;
        using (var stream = File.Create(temp))
        {
            ExplorationStore.Write(stream, snapshot);
            size = stream.Length;
        }
        File.Move(temp, path, overwrite: true);
        _savedCells = Cells();
        _savedSeenVersion = ExplorationProbe.SeenMemory.Version;
        Plugin.Log.LogInfo($"[Exploration saved] saved key={_key} scopes={snapshot.Explored.Count}+{snapshot.Seen.Count} cells={snapshot.Explored.Values.Sum(p => p.Count)} places={snapshot.Seen.Values.Sum(k => k.Count)} bytes={size}");
    }

    private static void Warn(string message)
    {
        if (message == _lastWarning) return;
        _lastWarning = message;
        Plugin.Log.LogWarning($"Exploration persistence: {message}");
    }
}
