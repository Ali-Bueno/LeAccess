using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Utils;
using LeAccess.Accessibility.WorldCues;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Landmarks;

// A shrine prefab has no hover text; its buff carries the game's localized shrine name (reference/exploration-mode.md).
internal static class ShrineBuffNames
{
    private const string ShrinePrefix = "Shrine";
    private static readonly Dictionary<string, string> Names = new();

    /// <summary>The localized shrine name of the buff that matches the shrine object's name, or null.</summary>
    internal static string Of(string shrineObjectName)
    {
        string token = InteractableNames.ShrineToken(shrineObjectName);
        if (token is null) return null;
        if (Names.Count == 0) Load();
        string id = InteractableNames.MatchShrineBuff(token, Names.Keys);
        return id is null ? null : Names[id];
    }

    private static void Load()
    {
        try
        {
            var plain = NonAilmentUIBuffList.get();
            var buffs = IsAlive(plain) ? plain.uiBuffs : null;
            for (int index = 0; buffs is not null && !buffs.WasCollected && index < buffs.Count; index++)
            {
                var buff = buffs[index];
                if (!IsAlive(buff) || !buff.id.ToString().StartsWith(ShrinePrefix, StringComparison.Ordinal)) continue;
                string raw = buff.getName() ?? "";
                Add(buff.id.ToString(), Localization.GetText(raw, raw));
            }
            var ailments = AilmentList.get();
            var shrineBuffs = IsAlive(ailments) ? ailments.shrineBuffs : null;
            for (int index = 0; shrineBuffs is not null && !shrineBuffs.WasCollected && index < shrineBuffs.Count; index++)
            {
                var ailment = shrineBuffs[index];
                if (IsAlive(ailment)) Add(ailment.id.ToString(), ailment.getLocalizedName(false));
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Shrine buff names unavailable: {ex.Message}");
        }
    }

    private static void Add(string id, string text)
    {
        string name = TextUtils.CleanText(text);
        if (!string.IsNullOrWhiteSpace(name)) Names[id] = name;
    }
}
