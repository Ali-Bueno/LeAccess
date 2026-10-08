using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.ScreenContent;

/// <summary>Reads a generic native modal's template header/body and its static text.</summary>
/// <remarks>See reference/announcement-lifecycle.md (generic modal windows).</remarks>
internal sealed class GenericModalText
{
    private readonly GameObject _root;
    // Bounds the per-frame template search while a modal opens (about two seconds at 60 fps).
    private const int MaxLateResolveAttempts = 120;
    private bool _resolved;
    private int _attempts;
    private TMP_Text _title;
    private TMP_Text _message;

    private readonly System.Func<Component, bool> _skip;
    private readonly System.Func<string> _extra;

    // A modal may name labels it reads itself (`skip`) and add that text (`extra`); see reference/faction-vendor-panels.md.
    internal GenericModalText(GameObject root, System.Func<Component, bool> skip = null, System.Func<string> extra = null)
    {
        _root = root;
        _skip = skip;
        _extra = extra;
    }

    internal string ReadOpening()
    {
        _resolved = false; _attempts = 0;
        Resolve();
        return ScreenContentTracker.Combine(new[]
            { ScreenContentReader.Read(_root, _title, _message, include: IsStatic), _extra?.Invoke() });
    }

    internal string ReadHeader()
    {
        Resolve();
        if (!UiVisibility.IsVisible(_root)) return null;
        return ScreenContentTracker.Combine(new[] { Read(_title), Read(_message), _extra?.Invoke() });
    }

    // Lists, scroll content and control captions belong to focus navigation, not to the window body.
    private bool IsStatic(Component label) =>
        !IsInside<Selectable>(label) && !IsInside<ScrollRect>(label) && !(_skip?.Invoke(label) ?? false);

    private bool IsInside<T>(Component label) where T : Component
    {
        var owner = label.GetComponentInParent<T>();
        return IsAlive(owner) && owner.transform.IsChildOf(_root.transform);
    }

    private static string Read(TMP_Text label) => TextUtils.IsVisibleLabel(label) ? TextUtils.CleanText(label.text) : null;

    // ConfirmationModalBase fills its single IModalTemplate through SetTitle/SetMessage.
    private void Resolve()
    {
        if (!UiVisibility.IsVisible(_root)) return;
        if (_resolved && IsAlive(_title) && IsAlive(_message) && _title.gameObject.activeInHierarchy) return;
        if (_attempts++ >= MaxLateResolveAttempts) return;
        _title = _message = null;
        int count = 0;
        TMP_Text title = null, message = null;
        foreach (var template in _root.GetComponentsInChildren<LE.UI.PanelSystem.ModalTemplate>(false))
            if (IsAlive(template) && ++count == 1) { title = template._title; message = template._message; }
        foreach (var template in _root.GetComponentsInChildren<LE.UI.Components.ModalTemplate>(false))
            if (IsAlive(template) && ++count == 1) { title = template._title; message = template._message; }
        if (count != 1) { _resolved = false; return; }
        _resolved = true;
        _title = title;
        _message = message;
    }
}
