using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.Accessibility.Players;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using NVector3 = System.Numerics.Vector3;

namespace LeAccess.Accessibility.Morditas;

/// <summary>The encounter's synced state as every player sees it; the game's tracker and threshold data.</summary>
internal readonly record struct MorditasReading(RageOfMorditasSync Sync, int Encounter, MorditasPhase Phase,
    bool Success, int HuntKills, float TimeLeft, int GauntletKills, NVector3 Centre, bool? Inside, bool AllOut);

/// <summary>Trail and circle values from RageOfMorditasData and the encounter config (reference/rage-of-morditas.md).</summary>
internal readonly record struct MorditasData(int[] Thresholds, float WarningSeconds, float CriticalSeconds,
    float OutOfZoneSeconds, float Radius, float TrailNear, float TrailFar, float TrailInterval);

// The encounter the visual-controller hooks last reported, read from its network object, which the host and online
// clients both hold (RageOfMorditasLogic exists only on the server). See reference/rage-of-morditas.md#hooks.
internal static class MorditasProbe
{
    // Keys of the game's UI string table (reference/rage-of-morditas.md#game-texts).
    private const string Prefix = "RageOfMorditas_Hunt_";
    private static readonly List<NVector3> Copied = new();
    private static RageOfMorditasSync _sync;
    private static int _encounter;
    private static bool? _inside;
    private static bool _allOut;
    private static string _lastFailure;

    internal static BloodTrails Trails { get; } = new();

    internal static void Track(RageOfMorditasSync sync)
    {
        try
        {
            if (!IsAlive(sync)) return;
            int id = sync.GetInstanceID();
            if (id == _encounter && IsAlive(_sync)) return;
            _sync = sync;
            _encounter = id;
            _inside = null;
            _allOut = false;
            Trails.Clear();
            // Claimed at the hook, before the game's own pop-up can settle in the generic reader.
            Claim();
            Plugin.Log.LogInfo($"[Morditas] tracking encounter {id}");
        }
        catch (Exception ex) { Fail(ex); }
    }

    internal static void Hunt(RageOfMorditasHuntVisualController hunt)
    {
        try { if (IsAlive(hunt)) Track(hunt._sync); }
        catch (Exception ex) { Fail(ex); }
    }

    internal static void Circle(RageOfMorditasGauntletVisualController gauntlet, bool? inside, bool? allOut)
    {
        try
        {
            if (!IsAlive(gauntlet) || !IsAlive(gauntlet._sync)) return;
            Track(gauntlet._sync);
            if (gauntlet._sync.GetInstanceID() != _encounter) return;
            if (inside is { } status) _inside = status;
            if (allOut is { } all) _allOut = all;
            if (inside is null && allOut is null) return;
            Plugin.Log.LogInfo($"[Morditas] circle inside={_inside?.ToString() ?? "unknown"} all_out={_allOut}");
        }
        catch (Exception ex) { Fail(ex); }
    }

    // The game passes its List<Vector3>; values are copied at once, the native list is not kept.
    internal static void TrailsDrawn(RageOfMorditasHuntVisualController hunt,
        Il2CppSystem.Collections.Generic.IReadOnlyList<Vector3> positions)
    {
        try
        {
            if (!IsAlive(hunt) || !IsAlive(hunt._sync) || positions is null || positions.WasCollected) return;
            Track(hunt._sync);
            if (hunt._sync.GetInstanceID() != _encounter) return;
            var list = positions.TryCast<Il2CppSystem.Collections.Generic.List<Vector3>>();
            if (list is null) { Fail(new InvalidCastException("blood trail positions are not a List<Vector3>")); return; }
            Copied.Clear();
            for (int index = 0; index < list.Count; index++) Copied.Add(GuidanceRouteProbe.Convert(list[index]));
            var player = LocalPlayerSource.Player;
            Trails.Refresh(_encounter, Copied,
                IsAlive(player) ? GuidanceRouteProbe.Convert(player.transform.position) : new NVector3(float.NaN));
        }
        catch (Exception ex) { Fail(ex); }
    }

    /// <summary>Leaves the encounter's pop-up sentences, widgets and instructions to the Morditas reader.</summary>
    internal static void Claim() => MorditasSpeechOwner.Begin(new[] { Text("EventStarted_Description"),
        Text("EventFailed_Description"), Text("RitualStart_Description"), Text("BloodrageChargeGained_Description") });

    internal static bool TryRead(out MorditasReading reading)
    {
        reading = default;
        if (_encounter == 0) return false;
        var sync = _sync;
        if (!IsAlive(sync)) { Forget(); return false; }
        if (!sync.IsLocalPlayerInEncounterScene) return false;
        var net = sync.networkObject;
        if (net is null || net.WasCollected || net.State > (byte)MorditasPhase.Finished) return false;
        reading = new(sync, _encounter, (MorditasPhase)net.State, net.Success, net.HuntKillCount,
            net.HuntTimeTotal - net.HuntTimePassed, net.GauntletKillCount, GuidanceRouteProbe.Convert(net.GauntletLocation),
            _inside, _allOut);
        return true;
    }

    internal static MorditasData ReadData(RageOfMorditasSync sync)
    {
        var data = RageOfMorditasData.Instance;
        var config = IsAlive(sync) ? sync.GetEncounterConfig() : null;
        float radius = IsAlive(config) ? config.GauntletAreaRadius : 0;
        if (!IsAlive(data)) return new(Array.Empty<int>(), 0, 0, 0, radius, 0, 0, 0);
        var list = data.HuntKillThresholdsForBloodrageCharges;
        var thresholds = new int[list is null || list.WasCollected ? 0 : list.Count];
        for (int index = 0; index < thresholds.Length; index++) thresholds[index] = list[index];
        return new(thresholds, data.HuntRemainingDurationWarning, data.HuntRemainingDurationCritical,
            data.GauntletOutOfZoneTimerDuration, radius, data.BloodTrailMinDistance, data.BloodTrailMaxDistance,
            data.BloodTrailFireInterval);
    }

    internal static MorditasGameTexts Texts() => new(Clean(Localization.GetText("RageOfMorditas_Name", "")),
        Text("EventStarted_Title"), Text("EventStarted_Description"), Text("RitualStart_Title"),
        Text("RitualStart_Description"), Text("Objective_PrepareGauntlet"), Text("Objective_ReadyForReward"),
        Text("BloodrageChargeGained_Title"), Text("EventFailed_Title"), Text("EventFailed_Description"));

    private static void Forget()
    {
        _sync = null;
        _encounter = 0;
        _inside = null;
        _allOut = false;
        Trails.Clear();
        Copied.Clear();
    }

    internal static void Reset()
    {
        Forget();
        _lastFailure = null;
    }

    private static string Text(string key) => Clean(Localization.GetText(Prefix + key, ""));

    private static string Clean(string text) => string.IsNullOrWhiteSpace(text) ? null : TextUtils.CleanText(text);

    private static void Fail(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"[Morditas] encounter reading unavailable: {ex.Message}");
    }
}
