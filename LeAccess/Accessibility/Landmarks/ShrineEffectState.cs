using System;
using System.Collections.Generic;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Landmarks;

/// <summary>One shrine buff on the HUD: a stable key, the game's localized name and description, and the time left.</summary>
internal readonly record struct ShrineBuff(string Key, string Name, string Description, float Remaining);

/// <summary>What to say once for an activation; Shrine and Buff may each be missing.</summary>
internal readonly record struct ShrineActivation(string Shrine, ShrineBuff? Buff);

/// <summary>Pairs a shrine's activation with the buff it grants and wording of the announcement; see reference/map-landmarks.md.</summary>
internal sealed class ShrineEffectState
{
    // Buff timers only count down, so a larger value than last time means the shrine was used again.
    internal const float RefreshGain = 1f;
    // How long a half-known activation (a name without a buff, or a buff without a name) waits for its other half.
    internal const double PairSeconds = 3;

    private readonly Dictionary<string, float> _remaining = new();
    private readonly List<(ShrineBuff Buff, double Since)> _buffs = new();
    private string _shrine;
    private double _shrineSince;
    private bool _baseline;
    private long _session;

    internal void Reset()
    {
        _remaining.Clear();
        _buffs.Clear();
        _shrine = null;
        _baseline = false;
        _session = 0;
    }

    /// <summary>Feeds one poll; the first poll of a session only records the buffs already active.</summary>
    internal IReadOnlyList<ShrineActivation> Observe(long session, double now, IReadOnlyList<ShrineBuff> buffs, string activatedShrine)
    {
        var result = new List<ShrineActivation>();
        if (!_baseline || session != _session)
        {
            Reset();
            _baseline = true;
            _session = session;
            foreach (var buff in buffs) _remaining[buff.Key] = buff.Remaining;
            return result;
        }
        var present = new HashSet<string>();
        foreach (var buff in buffs)
        {
            present.Add(buff.Key);
            bool isNew = !_remaining.TryGetValue(buff.Key, out float before) || buff.Remaining > before + RefreshGain;
            _remaining[buff.Key] = buff.Remaining;
            if (isNew) _buffs.Add((buff, now));
        }
        foreach (string key in new List<string>(_remaining.Keys))
            if (!present.Contains(key)) _remaining.Remove(key);
        if (activatedShrine != null && _shrine == null) { _shrine = activatedShrine; _shrineSince = now; }

        while (_buffs.Count > 0 && (_shrine != null || now - _buffs[0].Since >= PairSeconds))
        {
            result.Add(new ShrineActivation(_shrine, _buffs[0].Buff));
            _buffs.RemoveAt(0);
            _shrine = null;
        }
        if (_shrine != null && now - _shrineSince >= PairSeconds)
        {
            result.Add(new ShrineActivation(_shrine, null));
            _shrine = null;
        }
        return result;
    }

    /// <summary>"Shrine: effect", the effect alone, or "Shrine activated" when no buff came with it.</summary>
    internal static string Text(ShrineActivation activation)
    {
        string effect = EffectText(activation.Buff);
        if (string.IsNullOrWhiteSpace(activation.Shrine)) return effect;
        return effect == null ? ModText.Format("shrine.activated", activation.Shrine)
            : ModText.Format("shrine.effect", activation.Shrine, effect);
    }

    internal static string EffectText(ShrineBuff? buff)
    {
        if (buff is not { } value) return null;
        string name = value.Name?.Trim(), description = value.Description?.Trim();
        if (string.IsNullOrEmpty(name)) return string.IsNullOrEmpty(description) ? null : description;
        if (string.IsNullOrEmpty(description) || string.Equals(name, description, StringComparison.OrdinalIgnoreCase)) return name;
        return name + ". " + description;
    }
}
