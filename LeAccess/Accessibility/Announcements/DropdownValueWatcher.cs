using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

/// <summary>Says a focused dropdown's new value once when it changed after its announcement (the list closed on a choice).</summary>
/// <remarks>Quiet when the focus announcement already spoke it. See <see cref="DropdownReader"/>.</remarks>
internal static class DropdownValueWatcher
{
    /// <summary>Once per frame, from the menu update.</summary>
    internal static void Poll()
    {
        try
        {
            if (!UiFocusOwnership.TryGetSelected(out var focused)) return;
            var dropdown = focused.GetComponent<TMP_Dropdown>() ?? focused.GetComponentInParent<TMP_Dropdown>();
            if (!IsAlive(dropdown) || dropdown.IsExpanded || !DropdownReader.ChangeSettled(dropdown, Time.unscaledTime)) return;
            string value = DropdownReader.Caption(dropdown);
            if (value == null || !ScreenReader.TrySpeak(value, interrupt: false)) return;
            DropdownReader.NoteSpoken(dropdown);
            Plugin.Log.LogInfo($"[Dropdown] {value}");
        }
        catch (System.Exception ex) { Plugin.Log.LogDebug($"Dropdown watcher: {ex.Message}"); }
    }
}
