using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Utils;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Vitals;

// The HUD debuff row belongs to the local player both online and offline.
internal static class VitalsEffectsProbe
{
    internal static string LastFailure { get; private set; }

    /// <summary>Reads the visible harmful effects as "Name" or "Name stacks", in HUD order.</summary>
    internal static List<string> Read()
    {
        var effects = new List<string>();
        LastFailure = null;
        try
        {
            var manager = BuffsUIManager.instance;
            if (!IsAlive(manager)) return effects;
            var debuffs = manager.debuffs;
            if (debuffs == null || debuffs.WasCollected) return effects;
            for (int index = 0; index < debuffs.Count; index++)
            {
                var entry = debuffs[index];
                if (!IsAlive(entry) || !entry.gameObject.activeInHierarchy) continue;
                var buff = entry.uiBuff;
                if (buff == null || buff.WasCollected) continue;
                string name = TextUtils.CleanText(LocalizedName(buff));
                if (string.IsNullOrWhiteSpace(name)) continue;
                string text = entry.stacks > 1 ? name + " " + entry.stacks : name;
                if (!effects.Contains(text)) effects.Add(text);
            }
        }
        catch (Exception ex) { LastFailure = ex.GetType().Name + ": " + ex.Message; }
        return effects;
    }

    // UIBuff.getName returns the raw display name; ailments also expose the game's localized one.
    private static string LocalizedName(UIBuff buff)
    {
        var ailment = buff.TryCast<Ailment>();
        string localized = IsAlive(ailment) ? ailment.getLocalizedName(false) : null;
        return string.IsNullOrWhiteSpace(localized) ? buff.getName() : localized;
    }
}
