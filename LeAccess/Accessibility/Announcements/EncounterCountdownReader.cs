using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

// This native countdown rewrites its entire localized instruction every second.
// Its coroutine identifies a restart even when the displayed widget stays open.
internal sealed class EncounterCountdownReader
{
    private readonly Dictionary<int, Dictionary<int, Entry>> _roots = new();
    private readonly ScreenContentTracker _tracker = new();

    internal string Read(GameObject root, bool canSpeak, List<string> announcements)
    {
        int rootId = root.GetInstanceID();
        if (!_roots.TryGetValue(rootId, out var entries))
            _roots[rootId] = entries = new();
        var visible = new HashSet<int>();
        string text = ScreenContentReader.Read(root, null, null, label =>
        {
            var owner = label.GetComponentInParent<LE.UI.NotificationSystem.RageOfMorditasOutOfGauntletNotification>();
            if (!IsAlive(owner) || !IsAlive(owner._countdownText) ||
                owner._countdownText.GetInstanceID() != label.GetInstanceID() ||
                !label.transform.IsChildOf(owner.transform)) return label.text;
            var coroutine = owner._currentCountdownCoroutine;
            if (coroutine is null || coroutine.WasCollected || coroutine.Pointer.ToInt64() == 0) return label.text;
            int id = label.GetInstanceID();
            long identity = coroutine.Pointer.ToInt64();
            visible.Add(id);
            if (!entries.TryGetValue(id, out var entry) || entry.CoroutineId != identity)
            {
                entries[id] = entry = new Entry { CoroutineId = identity };
                _tracker.Hide(id);
            }
            if (!entry.Spoken && entry.ObservedFrame != Time.frameCount)
            {
                entry.ObservedFrame = Time.frameCount;
                _tracker.Observe(id, TextUtils.CleanText(label.text), canSpeak, out string announcement);
                if (announcement != null)
                {
                    entry.Spoken = true;
                    announcements.Add(announcement);
                }
            }
            // Other labels keep their own normal two-frame settling and changes.
            return null;
        });
        var expired = new List<int>();
        foreach (int id in entries.Keys)
            if (!visible.Contains(id)) expired.Add(id);
        foreach (int id in expired) { entries.Remove(id); _tracker.Hide(id); }
        return text;
    }

    internal void Hide(int rootId)
    {
        if (!_roots.TryGetValue(rootId, out var entries)) return;
        foreach (int id in entries.Keys) _tracker.Hide(id);
        _roots.Remove(rootId);
    }

    internal void Reset() { _roots.Clear(); _tracker.Reset(); }

    private sealed class Entry
    {
        internal long CoroutineId;
        internal bool Spoken;
        internal int ObservedFrame = -1;
    }
}
