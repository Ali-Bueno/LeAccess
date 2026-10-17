using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using LeAccess.I18n;

namespace LeAccess.Accessibility.ModMenu;

/// <summary>An object sound: Cross or Square plays it, left/right change its level or distance.</summary>
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

/// <summary>A sound to listen to whose level is set by its page's options: Cross or Square plays it.</summary>
internal sealed class PreviewItem : MenuItem
{
    private readonly string _id;
    internal PreviewItem(string id) => _id = id;
    internal override string Label => ModText.Format("menu.listen", ModText.Get($"sound.{_id}"));
    internal override string Details => string.Join(" ", ModText.Get($"sound.{_id}.details"), ModText.Get("menu.preview_hint"));
    internal override MenuReply Activate() { Preview(); return default; }

    internal override bool Preview()
    {
        SoundPreview.Play(_id);
        return true;
    }
}

/// <summary>The sounds of a section page: each object kind's volume and distance, or the sounds its options set.</summary>
internal static class ModMenuSoundPages
{
    internal static IEnumerable<MenuItem> Extras(ConfigFile config, SectionSpec section)
    {
        if (section.Section == ModMenuCatalog.WorldSection) return WorldPages(config);
        var keys = new HashSet<string>();
        foreach (var spec in section.Options) keys.Add(spec.Key);
        var previews = new List<MenuItem>();
        foreach (var entry in SoundGlossary.Entries)
            if (entry.VolumeSection == section.Section && keys.Contains(entry.VolumeKey)) previews.Add(new PreviewItem(entry.Id));
        return previews;
    }

    private static IEnumerable<MenuItem> WorldPages(ConfigFile config)
    {
        var volumes = new List<MenuItem>();
        var distances = new List<MenuItem>();
        var group = ModMenuPages.Find(config, ModMenuCatalog.WorldSection, ModMenuCatalog.GroupRange.Key);
        MenuItem groupItem = group is null ? null : ModMenuPages.Item(group, ModMenuCatalog.GroupRange);
        foreach (var entry in SoundGlossary.Entries)
        {
            if (entry.VolumeSection == ModMenuCatalog.WorldVolumesSection &&
                ModMenuPages.Find(config, entry.VolumeSection, entry.VolumeKey) is { } volume)
            {
                var setting = new ConfigSetting(volume);
                double step = setting.Range is { Maximum: > 100 } ? 10 : 5;
                volumes.Add(new SoundItem(entry.Id, setting, new SettingSpec(entry.VolumeKey, null, SettingUnit.Percent, step),
                    "menu.sound_hint"));
            }
            if (entry.RangeKey is not null && ModMenuPages.Find(config, entry.RangeSection, entry.RangeKey) is { } range)
                distances.Add(new SoundItem(entry.Id, new ConfigSetting(range), ModMenuCatalog.KindRange, "menu.distance_hint",
                    () => ModText.Format("menu.value.group_distance", groupItem?.Value)));
        }
        if (volumes.Count > 0) yield return new PageItem(new MenuPage(ModText.Get("menu.world_volumes"), volumes));
        if (distances.Count > 0) yield return new PageItem(new MenuPage(ModText.Get("menu.distances"), distances));
    }
}
