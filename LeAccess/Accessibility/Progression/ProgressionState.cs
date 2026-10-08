using System;
using System.Collections.Generic;
using System.Linq;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Progression;

internal readonly record struct SkillPoint(string Key, string Name);

/// <summary>One read of the local character's progression; PassivePoints is the unspent count.</summary>
internal readonly record struct ProgressionSnapshot(long Session, int Level, int MaxLevel, long Experience, long NextExperience,
    int PassivePoints, IReadOnlyList<SkillPoint> Skills, bool NewSkill, string Zone);

/// <summary>Change detection and wording for level-ups, passive points and skills that can take a point.</summary>
internal sealed class ProgressionState
{
    private enum Kind { Level, Passive, Skill, NewSkill }

    private readonly record struct Pending(Kind Kind, string Key, string Text);

    private readonly List<Pending> _pending = new();
    private HashSet<string> _skills = new();
    private bool _hasBaseline, _newSkill;
    private long _session;
    private int _level, _passive;

    internal bool HasPending => _pending.Count > 0;
    internal string Next => _pending.Count > 0 ? _pending[0].Text : null;

    internal void Acknowledge()
    {
        if (_pending.Count > 0) _pending.RemoveAt(0);
    }

    internal void Reset()
    {
        _pending.Clear();
        _skills = new HashSet<string>();
        _hasBaseline = _newSkill = false;
        _session = 0;
        _level = _passive = 0;
    }

    // The first read of a session (or of a new character) is a silent baseline.
    internal void Observe(ProgressionSnapshot snapshot)
    {
        if (!_hasBaseline || snapshot.Session != _session)
        {
            Reset();
            _hasBaseline = true;
            _session = snapshot.Session;
            _level = snapshot.Level;
            _passive = Math.Max(0, snapshot.PassivePoints);
            _skills = snapshot.Skills.Select(skill => skill.Key).ToHashSet();
            _newSkill = snapshot.NewSkill;
            return;
        }
        if (snapshot.Level > _level) Replace(Kind.Level, null, ModText.Format("progression.level_up", snapshot.Level));
        _level = snapshot.Level;
        ObservePassive(Math.Max(0, snapshot.PassivePoints));
        ObserveSkills(snapshot.Skills);
        if (snapshot.NewSkill && !_newSkill) Replace(Kind.NewSkill, null, ModText.Get("progression.new_skill"));
        else if (!snapshot.NewSkill) Remove(Kind.NewSkill, null);
        _newSkill = snapshot.NewSkill;
    }

    private void ObservePassive(int points)
    {
        if (points > _passive) Replace(Kind.Passive, null, PassiveText(points));
        else if (points < _passive) Remove(Kind.Passive, null);
        _passive = points;
    }

    private void ObserveSkills(IReadOnlyList<SkillPoint> skills)
    {
        var current = skills.Select(skill => skill.Key).ToHashSet();
        foreach (var skill in skills)
            if (!_skills.Contains(skill.Key))
                _pending.Add(new Pending(Kind.Skill, skill.Key, ModText.Format("progression.skill_can_level", skill.Name)));
        foreach (string gone in _skills.Where(key => !current.Contains(key))) Remove(Kind.Skill, gone);
        _skills = current;
    }

    private void Replace(Kind kind, string key, string text)
    {
        Remove(kind, key);
        _pending.Add(new Pending(kind, key, text));
    }

    private void Remove(Kind kind, string key) => _pending.RemoveAll(item => item.Kind == kind && item.Key == key);

    internal static string PassiveText(int points) => points > 1
        ? ModText.Format("progression.passive_points", points) : ModText.Get("progression.passive_point");

    /// <summary>The on-demand report: level, experience, unspent points, skills and zone.</summary>
    internal static string StatusText(ProgressionSnapshot snapshot)
    {
        var parts = new List<string> { ModText.Format("progression.level", snapshot.Level) };
        if (snapshot.MaxLevel > 0 && snapshot.Level >= snapshot.MaxLevel) parts.Add(ModText.Get("progression.max_level"));
        else if (Percent(snapshot.Experience, snapshot.NextExperience) is int percent and >= 0)
            parts.Add(ModText.Format("progression.experience", percent));
        if (snapshot.PassivePoints > 0) parts.Add(PassiveText(snapshot.PassivePoints));
        if (snapshot.Skills.Count == 1) parts.Add(ModText.Format("progression.skill_can_level", snapshot.Skills[0].Name));
        else if (snapshot.Skills.Count > 1)
            parts.Add(ModText.Format("progression.skills_can_level", string.Join(", ", snapshot.Skills.Select(skill => skill.Name))));
        if (snapshot.NewSkill) parts.Add(ModText.Get("progression.new_skill"));
        if (!string.IsNullOrWhiteSpace(snapshot.Zone)) parts.Add(ModText.Format("progression.zone", snapshot.Zone));
        return string.Join(". ", parts);
    }

    /// <summary>Whole percent toward the next level, or -1 when the game gives no usable threshold.</summary>
    internal static int Percent(long experience, long next) =>
        next <= 0 ? -1 : (int)Math.Clamp(Math.Floor(Math.Max(0, experience) * 100.0 / next), 0, 100);
}
