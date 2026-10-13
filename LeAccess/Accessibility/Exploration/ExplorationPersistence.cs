using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EHG.Multiplayer;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Players;

namespace LeAccess.Accessibility.Exploration;

// Saves the explored ground and seen places (fixed-layout scenes only) all characters share between game runs.
internal static class ExplorationPersistence
{
    private const double SaveSeconds = 30;
    private static string _key;
    private static bool _sharedLoaded, _migrated;
    private static long _session;
    private static int _savedCells, _savedSeenVersion;
    private static double _nextSave;
    private static string _lastWarning, _lastTrace;

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
        _sharedLoaded = false;
        _session = 0;
        _savedCells = _savedSeenVersion = 0;
        _nextSave = 0;
        _lastWarning = _lastTrace = null;
    }

    // Online, PlayerFinder's character data can lack the name or class (see reference/exploration-mode.md),
    // so each identity part is read from the first source that has it.
    private static string CurrentKey()
    {
        bool online = GameplayEnvironment.IsOnlineClient;
        string name = null, account = null;
        int? classId = null;
        string trace = null;
        try
        {
            var character = PlayerFinder.getPlayerData();
            bool hasData = character is not null && !character.WasCollected;
            if (hasData)
            {
                name = character.CharacterName;
                classId = character.CharacterClass;
            }
            if (online)
            {
                if (string.IsNullOrWhiteSpace(name)) name = TrackerName();
                if (string.IsNullOrWhiteSpace(name)) name = InfoProviderName();
                account = AccountName();
                trace = $"data={(hasData ? "present" : "absent")} name='{name}' class={classId?.ToString() ?? "none"} account='{account}'";
            }
        }
        catch (Exception ex) { Warn($"identity read failed: {ex.Message}"); }
        string key = ExplorationKey.Compose(online, name, classId, account);
        if (online && key != _lastTrace)
        {
            _lastTrace = key;
            Plugin.Log.LogInfo($"[Exploration saved] online identity key={key ?? "none"} {trace}");
        }
        return key;
    }

    private static string TrackerName()
    {
        var tracker = PlayerFinder.getPlayerDataTracker();
        return tracker is null || tracker.WasCollected ? null : tracker.getCharacterName();
    }

    private static string InfoProviderName() =>
        PlayerFinder.TryGetLocalPlayerInfo(out var info) && info is not null && !info.WasCollected ? info.getCharacterName() : null;

    private static string AccountName()
    {
        var identity = LocalPlayerSource.Visuals?.UserIdentity;
        return identity is null || identity.WasCollected ? null : identity.Username;
    }

    private static string Folder() => Path.Combine(BepInEx.Paths.ConfigPath, "LeAccess", "Exploration");

    private static bool IsDirty() => Cells() != _savedCells || ExplorationProbe.SeenMemory.Version != _savedSeenVersion;

    private static int Cells() =>
        ExplorationBeacon.ExploredGround.FixedScopes.Sum(scope => scope.Value.CellCount);

    // Ground and places are shared by every character and stay in memory across a switch;
    // only the last death place belongs to the character.
    private static void Switch(string key)
    {
        if (!_sharedLoaded)
        {
            ExplorationBeacon.ExploredGround.Clear();
            ExplorationProbe.SeenMemory.Clear();
            ExplorationBeacon.ForgetMap();
            _migrated = LoadShared();
            _sharedLoaded = true;
        }
        else
        {
            foreach (var scope in ExplorationProbe.SeenMemory.FixedScopes()) scope.Value.RemoveWhere(ExplorationShared.IsDeath);
        }
        _key = key;
        LoadDeaths(key);
        _savedCells = _migrated ? -1 : Cells();
        _savedSeenVersion = ExplorationProbe.SeenMemory.Version;
        _migrated = false;
    }

    // True when the shared file was built from the old per-character files and still has to be written.
    private static bool LoadShared()
    {
        string folder = Folder(), path = ExplorationShared.SharedPath(folder);
        try
        {
            ExplorationSnapshot snapshot;
            string source;
            if (File.Exists(path) && TryRead(path, out snapshot)) source = "shared";
            else
            {
                snapshot = ExplorationShared.MergeLegacy(folder, Quarantine, out int files);
                source = $"merged {files} character files";
            }
            Import(snapshot, source);
            return source != "shared";
        }
        catch (Exception ex) { Warn($"load failed for the shared file: {ex.Message}"); }
        return false;
    }

    // A character's death places, from its own file or, before that exists, from its old whole-character file.
    private static void LoadDeaths(string key)
    {
        string folder = Folder();
        try
        {
            string path = ExplorationShared.DeathsPath(folder, key);
            if (!File.Exists(path)) path = ExplorationShared.LegacyPath(folder, key);
            if (!File.Exists(path) || !TryRead(path, out var snapshot)) return;
            Import(ExplorationShared.DeathsOf(snapshot), $"deaths of {key}");
        }
        catch (Exception ex) { Warn($"load failed for {key}: {ex.Message}"); }
    }

    private static bool TryRead(string path, out ExplorationSnapshot snapshot)
    {
        if (ExplorationShared.TryReadFile(path, out snapshot, out string reason)) return true;
        Quarantine(path, reason);
        return false;
    }

    private static void Import(ExplorationSnapshot snapshot, string what)
    {
        int cells = 0, places = 0;
        foreach (var scope in snapshot.Explored) cells += ExplorationBeacon.ExploredGround.ImportFixed(scope.Key, scope.Value);
        foreach (var scope in snapshot.Seen) places += ExplorationProbe.SeenMemory.ImportFixed(scope.Key, scope.Value);
        Plugin.Log.LogInfo($"[Exploration saved] loaded {what} scopes={snapshot.Explored.Count}+{snapshot.Seen.Count} cells={cells} places={places}");
    }

    private static void Quarantine(string path, string reason)
    {
        Warn($"corrupt exploration file ({reason}); set aside as {Path.GetFileName(path)}.corrupt");
        try { File.Move(path, path + ".corrupt", overwrite: true); }
        catch (Exception ex) { Warn($"could not set the corrupt file aside: {ex.Message}"); }
    }

    private static void Save()
    {
        var shared = new ExplorationSnapshot();
        var deaths = new ExplorationSnapshot();
        foreach (var scope in ExplorationBeacon.ExploredGround.FixedScopes)
            if (scope.Value.CellCount > 0) shared.Explored[scope.Key] = scope.Value.Points.ToList();
        foreach (var scope in ExplorationProbe.SeenMemory.FixedScopes())
        {
            var keys = scope.Value.Where(ExplorationSeenMemory.Persistent).ToList();
            var own = keys.Where(ExplorationShared.IsDeath).ToList();
            keys.RemoveAll(ExplorationShared.IsDeath);
            if (keys.Count > 0) shared.Seen[scope.Key] = keys;
            if (own.Count > 0) deaths.Seen[scope.Key] = own;
        }
        string folder = Folder();
        Directory.CreateDirectory(folder);
        long size = Write(ExplorationShared.SharedPath(folder), shared);
        if (deaths.Seen.Count > 0) Write(ExplorationShared.DeathsPath(folder, _key), deaths);
        _savedCells = Cells();
        _savedSeenVersion = ExplorationProbe.SeenMemory.Version;
        Plugin.Log.LogInfo($"[Exploration saved] saved key={_key} scopes={shared.Explored.Count}+{shared.Seen.Count} cells={shared.Explored.Values.Sum(p => p.Count)} places={shared.Seen.Values.Sum(k => k.Count)} bytes={size}");
    }

    private static long Write(string path, ExplorationSnapshot snapshot)
    {
        string temp = path + ".tmp";
        long size;
        using (var stream = File.Create(temp))
        {
            ExplorationStore.Write(stream, snapshot);
            size = stream.Length;
        }
        File.Move(temp, path, overwrite: true);
        return size;
    }

    private static void Warn(string message)
    {
        if (message == _lastWarning) return;
        _lastWarning = message;
        Plugin.Log.LogWarning($"Exploration persistence: {message}");
    }
}
