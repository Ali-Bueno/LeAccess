using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using LeAccess.I18n;

namespace LeAccess.Accessibility.ModMenu;

/// <summary>A sound of the glossary: Cross or Square plays it, left/right change its level or distance.</summary>
internal sealed class SoundItem : MenuItem
{
    private readonly string _id, _hintKey;
    private readonly ISetting _setting;
    private readonly SettingItem _value;
    private readonly Func<string> _zeroText;

    internal SoundItem(string id, ISetting setting, SettingSpec spec, string hintKey, Func<string> zeroText = null)
    {
        _id = id;
        _setting = setting;
        _hintKey = hintKey;
        _zeroText = zeroText;
        _value = new SettingItem(setting, spec, null, null);
    }

    internal override string Label => ModText.Get($"sound.{_id}");
    internal override string Value =>
        _zeroText != null && Convert.ToDouble(_setting.Value) == 0 ? _zeroText() : _value.Value;
    internal override string Details =>
        string.Join(" ", ModText.Get($"sound.{_id}.details"), ModText.Get(_hintKey), _value.Details);
    internal override MenuReply Activate() { Preview(); return default; }
    internal override bool Adjust(int direction) => _value.Adjust(direction);
    internal override bool Reset() => _value.Reset();

    internal override bool Preview()
    {
        SoundPreview.Play(_id);
        return true;
    }
}

/// <summary>The glossary with each sound's volume, and the object distances.</summary>
internal static class ModMenuSoundPages
{
    internal static IEnumerable<MenuItem> Build(ConfigFile config)
    {
        var sounds = new List<MenuItem>();
        var distances = new List<MenuItem>();
        var group = ModMenuPages.Find(config, ModMenuCatalog.WorldSection, ModMenuCatalog.GroupRange.Key);
        MenuItem groupItem = group is null ? null : ModMenuPages.Item(group, ModMenuCatalog.GroupRange);
        if (groupItem is not null) distances.Add(groupItem);
        foreach (var entry in SoundGlossary.Entries)
        {
            if (ModMenuPages.Find(config, entry.VolumeSection, entry.VolumeKey) is { } volume)
            {
                var setting = new ConfigSetting(volume);
                double step = setting.Range is { Maximum: > 100 } ? 10 : 5;
                sounds.Add(new SoundItem(entry.Id, setting, new SettingSpec(entry.VolumeKey, null, SettingUnit.Percent, step),
                    "menu.sound_hint"));
            }
            if (entry.RangeKey is not null && ModMenuPages.Find(config, entry.RangeSection, entry.RangeKey) is { } range)
                distances.Add(new SoundItem(entry.Id, new ConfigSetting(range), ModMenuCatalog.KindRange, "menu.distance_hint",
                    () => ModText.Format("menu.value.group_distance", groupItem?.Value)));
        }
        if (sounds.Count > 0) yield return new PageItem(new MenuPage(ModText.Get("menu.sounds"), sounds));
        if (distances.Count > 0) yield return new PageItem(new MenuPage(ModText.Get("menu.distances"), distances));
    }
}
