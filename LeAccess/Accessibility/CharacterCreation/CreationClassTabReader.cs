using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>Reads character-creation descriptions through each tab's declared content owner.</summary>
internal static class CreationClassTabReader
{
    // These semantic localization prefixes are serialized in the installed creation prefab.
    private const string ClassDescriptionPrefix = "Class_Description_";
    private const string MasteryDescriptionPrefix = "Mastery_Description_";
    private static readonly List<Entry> Entries = new();
    private static CharacterSelect _character;

    internal static bool TryRead(GameObject go, out ClassSelection selection)
    {
        selection = default;
        var character = CharacterSelect.instance;
        if (!IsAlive(character) || character.currentState != CharacterSelect.CharacterSelectState.NameCreation ||
            !UiVisibility.IsVisible(character.gameObject)) return false;
        var tab = go.GetComponentInParent<TabUIElement>();
        if (!IsAlive(tab) || !tab.isActiveAndEnabled || !tab.transform.IsChildOf(character.transform)) return false;
        Cache(character);
        foreach (var entry in Entries)
        {
            if (!IsOwned(entry) || entry.Tab.Pointer != tab.Pointer) continue;
            selection = new($"creation-tab:{tab.Pointer}", tab.gameObject,
                Read(entry.Name), Read(entry.Description));
            return true;
        }
        return false;
    }

    internal static string ReadBaseDescription(CharacterSelect character, CharacterClassID classId)
    {
        Cache(character);
        string key = ClassDescriptionPrefix + classId;
        foreach (var entry in Entries)
            if (IsOwned(entry) && entry.Description.Key == key) return Read(entry.Description);
        return null;
    }

    private static void Cache(CharacterSelect character)
    {
        if (IsAlive(_character) && _character.Pointer == character.Pointer) return;
        Reset();
        _character = character;
        foreach (var tab in character.GetComponentsInChildren<TabUIElement>(true))
        {
            if (!IsAlive(tab) || !IsAlive(tab.controller)) continue;
            var contents = tab.controller.tabContents;
            int index = tab.elementID;
            if (contents is null || contents.WasCollected || index < 0 || index >= contents.Count) continue;
            var content = contents[index];
            if (!IsAlive(content) || !content.transform.IsChildOf(character.transform)) continue;
            var description = Find(content, description: true);
            var name = Find(tab.gameObject, description: false);
            if (description != null && name != null) Entries.Add(new(tab, content, name, description));
        }
    }

    private static TextSource Find(GameObject root, bool description)
    {
        TextSource result = null;
        foreach (var source in root.GetComponentsInChildren<LocalizeStringEvent>(true))
        {
            if (!IsAlive(source)) continue;
            var reference = source.StringReference;
            if (reference is null || reference.WasCollected) continue;
            var entry = reference.TableEntryReference;
            if (entry is null || entry.WasCollected) continue;
            string key = entry.Key;
            bool matches = description
                ? HasPrefix(key, ClassDescriptionPrefix) || HasPrefix(key, MasteryDescriptionPrefix)
                : (HasPrefix(key, "Class_") || HasPrefix(key, "Mastery_")) && !HasPrefix(key, "Class_Tab_");
            var text = source.GetComponent<TMP_Text>();
            if (!matches || !IsAlive(text)) continue;
            if (result != null && result.Key != key) return null;
            result = new(key, text);
        }
        return result;
    }

    private static bool IsOwned(Entry entry)
    {
        if (!IsAlive(_character) || !IsAlive(entry.Tab) || !IsAlive(entry.Tab.controller) ||
            !IsAlive(entry.Content) || !entry.Tab.transform.IsChildOf(_character.transform) ||
            !entry.Content.transform.IsChildOf(_character.transform)) return false;
        var contents = entry.Tab.controller.tabContents;
        int index = entry.Tab.elementID;
        return contents is { WasCollected: false } && index >= 0 && index < contents.Count &&
            IsAlive(contents[index]) && contents[index].Pointer == entry.Content.Pointer;
    }

    private static string Read(TextSource source) => !IsAlive(source.Label) ? null :
        TextUtils.CleanText(Localization.GetText(source.Key, source.Label.text ?? ""));

    private static bool HasPrefix(string value, string prefix) =>
        value?.StartsWith(prefix, StringComparison.Ordinal) == true;

    internal static void Reset() { _character = null; Entries.Clear(); }

    private sealed class TextSource
    {
        internal readonly string Key;
        internal readonly TMP_Text Label;
        internal TextSource(string key, TMP_Text label) { Key = key; Label = label; }
    }

    private sealed class Entry
    {
        internal readonly TabUIElement Tab;
        internal readonly GameObject Content;
        internal readonly TextSource Name, Description;
        internal Entry(TabUIElement tab, GameObject content, TextSource name, TextSource description)
        { Tab = tab; Content = content; Name = name; Description = description; }
    }
}
