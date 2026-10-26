using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Vitals;

/// <summary>Reads the minion cards of the game's HUD one per press (Create+R1 or Ctrl+M).</summary>
/// <remarks>See reference/minion-health.md.</remarks>
internal static class MinionHealthReport
{
    private readonly record struct Shown(Vector3 Position, MinionCardInfo Card, string Source);

    private static readonly MinionHealthStepper Stepper = new();
    private static MinionUI _ui;
    private static string _lastFailure;

    internal static string Next()
    {
        try
        {
            var shown = Read();
            int index = Stepper.Step(shown.Count, Time.unscaledTimeAsDouble);
            if (index < 0)
            {
                Plugin.Log.LogInfo($"[Minion health] no cards (hud={IsAlive(_ui)})");
                return ModText.Get("minions.none");
            }
            var cards = shown.ConvertAll(entry => entry.Card);
            string text = ScreenContentTracker.Combine(new[]
                { MinionHealthStepper.Describe(cards, index), ListPosition.Text(index + 1, cards.Count) });
            Plugin.Log.LogInfo($"[Minion health] {text} ({shown[index].Source})");
            return text;
        }
        catch (Exception ex)
        {
            Stepper.Reset();
            if (_lastFailure != ex.Message)
            {
                _lastFailure = ex.Message;
                Plugin.Log.LogWarning($"Minion health unavailable: {ex.Message}");
            }
            return ModText.Get("minions.unavailable");
        }
    }

    internal static void Reset()
    {
        Stepper.Reset();
        _ui = null;
    }

    // The cards as the HUD lays them out: rows top to bottom, then left to right.
    private static List<Shown> Read()
    {
        var shown = new List<Shown>();
        if (!IsAlive(_ui)) _ui = UnityEngine.Object.FindObjectOfType<MinionUI>();
        if (!IsAlive(_ui)) return shown;
        var singles = _ui.cards;
        if (singles is not null && !singles.WasCollected)
            foreach (var card in singles.Keys)
                if (Visible(card) && TrySingle(card, out var entry)) shown.Add(entry);
        var groups = _ui.multiMinionCards;
        if (groups is not null && !groups.WasCollected)
            foreach (var card in groups)
                if (Visible(card) && card.currentMinionCount > 0)
                    shown.Add(new Shown(card.transform.position,
                        new MinionCardInfo(Name(null, card.ability), 0, card.currentMinionCount), "group"));
        shown.Sort((a, b) => a.Position.y != b.Position.y ? b.Position.y.CompareTo(a.Position.y) : a.Position.x.CompareTo(b.Position.x));
        return shown;
    }

    private static bool TrySingle(SingleMinionCard card, out Shown entry)
    {
        entry = default;
        var visuals = card.minionVisuals;
        if (IsAlive(visuals) && visuals.dead) return false;
        // The card's own bar as drawn; the minion's overhead fill when the bar has no range yet.
        var bar = card.healthBar;
        int percent;
        string source;
        if (IsAlive(bar) && bar.maxFill > 0)
        {
            percent = VitalsAnnouncementState.Percent(bar.currentFill, bar.maxFill);
            source = $"bar {bar.currentFill:0.##}/{bar.maxFill:0.##}";
        }
        else
        {
            var overhead = IsAlive(visuals) ? visuals.TryCast<IOverheadBar>() : null;
            if (overhead is null) return false;
            percent = VitalsAnnouncementState.Percent(overhead.HealthNormalized, 1);
            source = $"overhead {overhead.HealthNormalized:0.###}";
        }
        string name = Name(IsAlive(visuals) ? NearestEnemyProbe.ReadName(visuals.BaseDisplayInformation) : null, card.ability);
        entry = new Shown(card.transform.position, new MinionCardInfo(name, percent, 0), source);
        return true;
    }

    // The minion's own name, else the summoning skill's.
    private static string Name(string minion, Ability ability)
    {
        if (!string.IsNullOrWhiteSpace(minion)) return minion;
        string skill = AbilityNames.Read(ability);
        return string.IsNullOrWhiteSpace(skill) ? ModText.Get("minions.unnamed") : skill;
    }

    private static bool Visible(MinionCard card) => IsAlive(card) && card.gameObject.activeInHierarchy;
}
