using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LeAccess.Accessibility.Players;
using LeAccess.Accessibility.Utils;
using LeAccess.Accessibility.WorldCues;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Landmarks;

/// <summary>Says a shrine's name and effect once after the player uses it; polled, no hook. See reference/map-landmarks.md.</summary>
internal static class ShrineEffectAnnouncer
{
    private const double PollSeconds = 0.25;
    private const string ShrinePrefix = "Shrine";
    private static readonly ShrineEffectState State = new();
    private static readonly Dictionary<int, bool> Activated = new();
    private static readonly List<ShrineBuff> Buffs = new();
    private static ConfigEntry<bool> _enabled;
    private static double _nextPoll;
    private static int _scene;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind("Landmarks", "AnnounceShrineEffects", true,
            "After you use a shrine, say its name and the effect it grants, from the game's own text.");
        Reset();
    }

    internal static void Update()
    {
        if (_enabled == null) return;
        try
        {
            if (!_enabled.Value || !Application.isFocused || LE.UI.LoadingScreen.IsVisible) { Reset(); return; }
            double now = Time.unscaledTimeAsDouble;
            if (now < _nextPoll) return;
            _nextPoll = now + PollSeconds;
            var scene = SceneManager.GetActiveScene();
            var player = LocalPlayerSource.Player;
            if (!scene.isLoaded || !IsAlive(player) || (int)player.scene.handle != (int)scene.handle) { Reset(); return; }
            if ((int)scene.handle != _scene) { Activated.Clear(); _scene = (int)scene.handle; }
            long session = ((long)(int)scene.handle << 32) | (uint)player.GetInstanceID();
            if (!ReadBuffs()) { Reset(); return; }
            foreach (var activation in State.Observe(session, now, Buffs, ReadActivation(scene, player.transform.position)))
            {
                string text = ShrineEffectState.Text(activation);
                if (string.IsNullOrWhiteSpace(text)) continue;
                ScreenReader.TrySpeak(text, interrupt: false);
                Plugin.Log.LogInfo($"[Shrine] effect: {text}");
            }
        }
        catch (Exception ex)
        {
            Reset();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Shrine effect feedback unavailable: {ex}");
        }
    }

    // The HUD buff row holds every active shrine effect with the game's own name and description.
    private static bool ReadBuffs()
    {
        Buffs.Clear();
        var manager = BuffsUIManager.instance;
        if (!IsAlive(manager)) return false;
        var entries = manager.buffs;
        if (entries is null || entries.WasCollected) return false;
        for (int index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            if (!IsAlive(entry) || !entry.gameObject.activeInHierarchy) continue;
            var buff = entry.uiBuff;
            if (buff is null || buff.WasCollected || !IsShrineBuff(buff, out string name)) continue;
            string raw = buff.getDescription() ?? "";
            Buffs.Add(new ShrineBuff($"{entry.isAilment}:{entry.id}", name, TextUtils.CleanText(Localization.GetText(raw, raw)),
                entry.maxRemainingDuration));
        }
        return true;
    }

    private static bool IsShrineBuff(UIBuff buff, out string name)
    {
        name = null;
        var ailment = buff.TryCast<Ailment>();
        if (IsAlive(ailment))
        {
            if (!AilmentList.IsShrineBuff(ailment)) return false;
            name = TextUtils.CleanText(ailment.getLocalizedName(false));
            return true;
        }
        var plain = buff.TryCast<NonAilmentUIBuff>();
        if (!IsAlive(plain) || !plain.id.ToString().StartsWith(ShrinePrefix, StringComparison.Ordinal)) return false;
        string raw = buff.getName() ?? "";
        name = TextUtils.CleanText(Localization.GetText(raw, raw));
        return true;
    }

    // The nearest shrine in reach whose network object just turned activated; its name is the game's hover text.
    private static string ReadActivation(Scene scene, Vector3 player)
    {
        if (!ShrinesManager.TryGetForScene(scene.name, out var manager) || !IsAlive(manager)) return null;
        var shrines = manager.GetAllShrines();
        if (shrines is null || shrines.WasCollected) return null;
        string name = null;
        float nearest = float.PositiveInfinity;
        bool found = false;
        for (int index = 0; index < shrines.Count; index++)
        {
            var shrine = shrines[index];
            if (!IsAlive(shrine) || shrine.NetObject is not { } net) continue;
            int id = shrine.GetInstanceID();
            bool now = net.IsActivated;
            bool known = Activated.TryGetValue(id, out bool before);
            Activated[id] = now;
            if (!known || before || !now) continue;
            var listener = shrine.GetComponentInChildren<WorldObjectClickListener>(true);
            float distance = IsAlive(listener) ? (listener.transform.position - player).sqrMagnitude : 0;
            float reach = IsAlive(listener) ? listener.interactionRange * listener.interactionRange : float.PositiveInfinity;
            if (distance > reach || distance >= nearest) continue;
            nearest = distance;
            found = true;
            name = InteractableRoles.GameShrineName(new[] { shrine.GetComponentInChildren<HoverTagOnMouseOver>(true),
                shrine.GetComponentInParent<HoverTagOnMouseOver>(true) });
        }
        if (found) Plugin.Log.LogInfo($"[Shrine] activated: {name ?? "unnamed"}");
        return found ? name ?? string.Empty : null;
    }

    private static void Reset()
    {
        State.Reset();
        Activated.Clear();
        Buffs.Clear();
        _nextPoll = 0;
        _scene = 0;
    }

    internal static void Cleanup()
    {
        Reset();
        _enabled = null;
        _lastFailure = null;
    }
}
