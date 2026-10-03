using System;
using System.Collections.Generic;
using System.Linq;

namespace LeAccess.Accessibility.Quests;

internal enum QuestProgressStatus { Active, Completed, Failed, Hidden }
internal enum QuestUpdateKind { Started, Updated, Completed, Failed }
internal readonly record struct QuestProgressSnapshot(string Key, string Title, QuestProgressStatus Status,
    string Revision, string Objectives, bool Available = true);
internal readonly record struct QuestProgressChange(QuestProgressSnapshot Quest, QuestUpdateKind Kind);

internal sealed class QuestUpdateState
{
    private readonly record struct PendingChange(QuestProgressChange Change, long Order);
    private readonly Dictionary<string, QuestProgressSnapshot> _accepted = new();
    private readonly Dictionary<string, QuestProgressSnapshot> _candidates = new();
    private readonly Dictionary<string, PendingChange> _pending = new();
    private readonly HashSet<string> _unavailable = new();
    private bool _initialised;
    private string _baseline;
    private long _source;
    private bool _sourceChanged;
    private string _sourceBaseline;
    private long _nextOrder;

    internal bool HasPendingContent => _pending.Keys.Any(key => !_unavailable.Contains(key));
    internal bool HasReadyContent => _pending.Keys.Any(key => !_unavailable.Contains(key) && !_candidates.ContainsKey(key));

    internal void Observe(IReadOnlyList<QuestProgressSnapshot> quests, long source = 0)
    {
        string signature = string.Join("\n", quests.OrderBy(q => q.Key, StringComparer.Ordinal)
            .Select(q => q.Key + ":" + q.Revision));
        if (source != _source)
        {
            _source = source; _sourceChanged = _initialised; _sourceBaseline = null;
            if (!_initialised) _baseline = null;
        }
        // Two complete observations settle initial synchronization without narrating saved history.
        if (!_initialised)
        {
            if (_baseline == signature)
            {
                foreach (var quest in quests) _accepted[quest.Key] = quest;
                _initialised = true;
            }
            _baseline = signature;
            return;
        }

        var present = new HashSet<string>();
        _unavailable.Clear();
        foreach (var quest in quests)
        {
            present.Add(quest.Key);
            bool existed = _accepted.TryGetValue(quest.Key, out var previous);
            if (!quest.Available)
            {
                _unavailable.Add(quest.Key); _candidates.Remove(quest.Key);
                if (!existed) _accepted[quest.Key] = quest;
                continue;
            }
            if (existed && !previous.Available || !existed && _sourceChanged)
            { _accepted[quest.Key] = quest; _candidates.Remove(quest.Key); continue; }
            if (existed && previous.Revision == quest.Revision)
            {
                _accepted[quest.Key] = quest;
                _candidates.Remove(quest.Key);
                if (_pending.TryGetValue(quest.Key, out var queued))
                    _pending[quest.Key] = queued with { Change = queued.Change with { Quest = quest } };
                continue;
            }
            if (!_candidates.TryGetValue(quest.Key, out var candidate) || candidate.Revision != quest.Revision)
            { _candidates[quest.Key] = quest; continue; }
            _candidates.Remove(quest.Key);
            _accepted[quest.Key] = quest;
            if (quest.Status == QuestProgressStatus.Hidden || !existed && quest.Status != QuestProgressStatus.Active)
            { _pending.Remove(quest.Key); continue; }
            var kind = quest.Status == QuestProgressStatus.Completed ? QuestUpdateKind.Completed
                : quest.Status == QuestProgressStatus.Failed ? QuestUpdateKind.Failed
                : !existed || previous.Status != QuestProgressStatus.Active ? QuestUpdateKind.Started : QuestUpdateKind.Updated;
            bool wasPending = _pending.TryGetValue(quest.Key, out var pending);
            if (wasPending && pending.Change.Kind == QuestUpdateKind.Started &&
                kind == QuestUpdateKind.Updated) kind = QuestUpdateKind.Started;
            _pending[quest.Key] = new(new(quest, kind), wasPending ? pending.Order : ++_nextOrder);
        }
        // Removal is not evidence of completion. Keep known state to ignore later list restoration.
        foreach (string key in _pending.Keys.Where(k => !present.Contains(k)).ToArray()) _pending.Remove(key);
        foreach (string key in _candidates.Keys.Where(k => !present.Contains(k)).ToArray()) _candidates.Remove(key);
        if (_sourceChanged && _sourceBaseline == signature) _sourceChanged = false;
        _sourceBaseline = signature;
    }

    internal bool TryTake(bool canSpeak, out QuestProgressChange change, bool acknowledge = true)
    {
        change = default;
        if (!canSpeak) return false;
        string selected = null;
        long oldest = long.MaxValue;
        foreach (var pair in _pending)
        {
            // Never deliver an older objective while its replacement is still settling.
            if (_candidates.ContainsKey(pair.Key) || _unavailable.Contains(pair.Key) || pair.Value.Order >= oldest) continue;
            selected = pair.Key;
            oldest = pair.Value.Order;
        }
        if (selected == null) return false;
        change = _pending[selected].Change;
        if (acknowledge) Acknowledge(change);
        return true;
    }

    internal bool Acknowledge(QuestProgressChange change)
    {
        string key = change.Quest.Key;
        if (_candidates.ContainsKey(key) || _unavailable.Contains(key) ||
            !_pending.TryGetValue(key, out var pending) || pending.Change != change) return false;
        _pending.Remove(key);
        return true;
    }

    internal void Reset()
    {
        _accepted.Clear(); _candidates.Clear(); _pending.Clear(); _unavailable.Clear();
        _initialised = false; _baseline = null; _source = 0; _sourceChanged = false; _sourceBaseline = null; _nextOrder = 0;
    }
}
