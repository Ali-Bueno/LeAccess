using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LeAccess.I18n;
using UnityEngine;

namespace LeAccess.Accessibility.ModMenu;

/// <summary>A BepInEx option seen through ISetting; setting it saves the config file.</summary>
internal sealed class ConfigSetting : ISetting
{
    private readonly ConfigEntryBase _entry;
    internal ConfigSetting(ConfigEntryBase entry) => _entry = entry;
    public Type Type => _entry.SettingType;
    public object Value { get => _entry.BoxedValue; set => _entry.BoxedValue = value; }
    public object DefaultValue => _entry.DefaultValue;

    public (double Minimum, double Maximum)? Range => _entry.Description?.AcceptableValues switch
    {
        AcceptableValueRange<int> range => (range.MinValue, range.MaxValue),
        AcceptableValueRange<float> range => (range.MinValue, range.MaxValue),
        AcceptableValueRange<double> range => (range.MinValue, range.MaxValue),
        _ => null
    };
}

/// <summary>A keyboard key option: Cross waits for the next key, Escape cancels.</summary>
internal sealed class KeyItem : MenuItem
{
    private readonly ConfigEntry<KeyCode> _entry;
    private readonly string _label, _details;

    internal KeyItem(ConfigEntry<KeyCode> entry, string label, string details)
    { _entry = entry; _label = label; _details = details; }

    internal override string Label => _label;
    internal override string Value => _entry.Value == KeyCode.None ? ModText.Get("menu.key_none") : _entry.Value.ToString();
    internal override string Details =>
        string.Join(" ", _details, ModText.Format("menu.default", ((KeyCode)_entry.DefaultValue).ToString()));
    internal override MenuReply Activate() => new(ModText.Get("menu.press_key"), CaptureKey: true);

    internal override bool Reset()
    {
        _entry.Value = (KeyCode)_entry.DefaultValue;
        return true;
    }

    internal void Set(KeyCode key) => _entry.Value = key;
}

/// <summary>Builds the menu's pages from the live config and the catalog, in the current speech language.</summary>
internal static class ModMenuPages
{
    private static readonly HashSet<string> LoggedUnlisted = new();

    internal static MenuPage Build(ConfigFile config, Func<MenuReply> close, IEnumerable<MenuItem> soundPages)
    {
        var root = new List<MenuItem>();
        if (Find(config, ModMenuCatalog.AudioSection, ModMenuCatalog.MasterVolume.Key) is { } master)
            root.Add(Item(master, ModMenuCatalog.MasterVolume));
        root.AddRange(soundPages);
        var listed = new HashSet<string>(ModMenuCatalog.SoundPageSections);
        foreach (var section in ModMenuCatalog.Sections)
        {
            listed.Add(section.Section);
            var items = new List<MenuItem>();
            var known = new HashSet<string>();
            foreach (var spec in section.Options)
            {
                known.Add(spec.Key);
                if (Find(config, section.Section, spec.Key) is { } entry) items.Add(Item(entry, spec));
            }
            items.AddRange(Unlisted(config, section.Section, known));
            if (items.Count > 0) root.Add(new PageItem(new MenuPage(ModText.Get($"menu.section.{section.Id}"), items)));
        }
        foreach (string section in Sections(config))
            if (!listed.Contains(section))
                root.Add(new PageItem(new MenuPage(section, Unlisted(config, section, new HashSet<string>()))));
        root.Add(new ActionItem(ModText.Get("menu.close"), close));
        return new MenuPage(ModText.Get("menu.title"), root);
    }

    internal static ConfigEntryBase Find(ConfigFile config, string section, string key)
    {
        var definition = new ConfigDefinition(section, key);
        return config.ContainsKey(definition) ? config[definition] : null;
    }

    internal static MenuItem Item(ConfigEntryBase entry, SettingSpec spec)
    {
        string label = ModText.Get($"menu.option.{spec.Id}");
        string details = ModText.Get($"menu.option.{spec.Id}.details");
        if (entry is ConfigEntry<KeyCode> key) return new KeyItem(key, label, details);
        return new SettingItem(new ConfigSetting(entry), spec, label, details,
            spec.Choices == ModMenuCatalog.Languages ? LanguageName : null);
    }

    private static string LanguageName(string code) =>
        code == "auto" ? ModText.Get("menu.language.auto") : ModText.GetOrDefault($"menu.language.{code}", code);

    // Options the catalog does not know yet still appear, under their own key and English description.
    private static List<MenuItem> Unlisted(ConfigFile config, string section, HashSet<string> known)
    {
        var items = new List<MenuItem>();
        foreach (var definition in config.Keys)
        {
            if (definition.Section != section || known.Contains(definition.Key)) continue;
            var entry = config[definition];
            if (LoggedUnlisted.Add($"{section}/{definition.Key}"))
                Plugin.Log.LogInfo($"[Mod menu] option without menu text: {section}/{definition.Key}");
            var spec = new SettingSpec(definition.Key, null);
            items.Add(entry is ConfigEntry<KeyCode> key ? new KeyItem(key, definition.Key, entry.Description?.Description)
                : new SettingItem(new ConfigSetting(entry), spec, definition.Key, entry.Description?.Description));
        }
        return items;
    }

    private static IEnumerable<string> Sections(ConfigFile config)
    {
        var seen = new HashSet<string>();
        foreach (var definition in config.Keys)
            if (seen.Add(definition.Section)) yield return definition.Section;
    }
}
