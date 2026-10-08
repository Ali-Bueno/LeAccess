using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

/// <summary>Dropdown wording shared by every panel: expanded options with state and place, title and value, value changes.</summary>
/// <remarks>See reference/faction-vendor-panels.md#bazaar-market.</remarks>
internal static class DropdownReader
{
    // A value that changed without a spoken announcement is said once it has stayed changed this long (the focus
    // announcement normally arrives within a frame or two and updates the noted value first).
    internal const float ChangeSettleSeconds = 0.3f;

    private static int _notedId;
    private static int _notedValue;
    private static int _pendingValue = int.MinValue;
    private static float _pendingSince;

    /// <summary>An option of an expanded list: its text, whether it is the chosen one and its place among the options.</summary>
    internal static string Option(string text, bool selected, int position, int count) =>
        ScreenContentTracker.Combine(new[] { text, selected ? ModText.Get("market.dropdown_selected") : null,
            count > 1 && position > 0 ? ModText.Format("list.position", position, count) : null });

    /// <summary>A collapsed dropdown (or field): its title and current value.</summary>
    internal static string Control(string title, string value)
    {
        bool hasTitle = !string.IsNullOrWhiteSpace(title), hasValue = !string.IsNullOrWhiteSpace(value);
        return hasTitle && hasValue ? ModText.Format("control.field_value", title, value) : hasTitle ? title : hasValue ? value : null;
    }

    /// <summary>The option the cursor is on inside an expanded list, with its selected state and "N of M".</summary>
    internal static string ReadOption(GameObject go)
    {
        string text = TextDetector.TryGetDropdownItemText(go);
        if (text == null) return null;
        var toggle = go.GetComponent<Toggle>() ?? go.GetComponentInParent<Toggle>();
        if (!IsAlive(toggle)) return text;
        Place(toggle, out int position, out int count);
        return Option(text, toggle.isOn, position, count);
    }

    // Options are the active toggles of one list; the inactive template item is not counted.
    private static void Place(Toggle toggle, out int position, out int count)
    {
        position = count = 0;
        var parent = toggle.transform.parent;
        if (!IsAlive(parent)) return;
        for (int i = 0; i < parent.childCount; i++)
        {
            var sibling = parent.GetChild(i).GetComponent<Toggle>();
            if (!IsAlive(sibling) || !sibling.gameObject.activeInHierarchy) continue;
            count++;
            if (sibling.Pointer == toggle.Pointer) position = count;
        }
    }

    /// <summary>The visible text of a title label that sits beside the control (a sibling, not part of it).</summary>
    internal static string Title(Component control)
    {
        var parent = IsAlive(control) ? control.transform.parent : null;
        if (!IsAlive(parent)) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.Pointer == control.transform.Pointer) continue;
            var label = child.GetComponent<TMP_Text>();
            if (TextUtils.IsVisibleLabel(label) && !string.IsNullOrWhiteSpace(label.text)) return TextUtils.CleanText(label.text);
        }
        return null;
    }

    /// <summary>The visible text of the value the collapsed dropdown shows.</summary>
    internal static string Caption(TMP_Dropdown dropdown) =>
        IsAlive(dropdown) && TextUtils.IsVisibleLabel(dropdown.captionText) && !string.IsNullOrWhiteSpace(dropdown.captionText.text)
            ? TextUtils.CleanText(dropdown.captionText.text) : null;

    /// <summary>Records that this dropdown's current value has been announced.</summary>
    internal static void NoteSpoken(TMP_Dropdown dropdown)
    {
        if (!IsAlive(dropdown)) return;
        _notedId = dropdown.GetInstanceID();
        _notedValue = dropdown.value;
        _pendingValue = int.MinValue;
    }

    /// <summary>True once an announced dropdown holds a different value that has settled; says it only once.</summary>
    internal static bool ChangeSettled(TMP_Dropdown dropdown, float now)
    {
        if (!IsAlive(dropdown) || dropdown.GetInstanceID() != _notedId || dropdown.value == _notedValue)
        { _pendingValue = int.MinValue; return false; }
        if (_pendingValue != dropdown.value) { _pendingValue = dropdown.value; _pendingSince = now; return false; }
        return now - _pendingSince >= ChangeSettleSeconds;
    }

    internal static void Reset() { _notedId = 0; _pendingValue = int.MinValue; }
}
