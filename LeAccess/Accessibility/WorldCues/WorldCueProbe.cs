using System;
using System.Collections.Generic;
using ItemFiltering;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Npcs;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.WorldCues;

internal sealed class WorldCueProbe
{
    private readonly HashSet<IntPtr> _lootListeners = new();
    private Camera _camera;
    private long _session;
    private Vector3 _position, _right, _forward;
    private IntPtr _selected;
    private bool _found, _preferred;
    private WorldCueTarget _nearest;
    private float _lootRange;
    private readonly bool[] _categoryPreferred = new bool[3];
    private readonly int[] _held = new int[3];
    /// <summary>Audible radius in metres for exits, waypoints and the stash; at most the native radius disables it.</summary>
    internal float LandmarkRange { get; set; }
    internal WorldCueTarget?[] Targets { get; } = new WorldCueTarget?[3];
    private int _coins, _equipment, _objects, _outOfRange, _filtered, _unavailable;
    private int _missingItemData, _nonEquipment, _missingLabel;
    internal string Diagnostics => $"registry={_coins}/{_equipment}/{_objects} outOfRange={_outOfRange} filtered={_filtered} unavailable={_unavailable} missingData={_missingItemData} nonEquipment={_nonEquipment} missingLabel={_missingLabel}";
    internal string LastFailure { get; private set; }
    internal bool HasWorld { get; private set; }
    internal long Session => _session;

    internal bool TryCapture(out WorldCueTarget target)
    {
        target = default;
        LastFailure = null;
        HasWorld = false;
        Array.Clear(Targets, 0, Targets.Length);
        Array.Clear(_categoryPreferred, 0, _categoryPreferred.Length);
        _coins = _equipment = _objects = _outOfRange = _filtered = _unavailable = 0;
        _missingItemData = _nonEquipment = _missingLabel = 0;
        var player = PlayerFinder.player;
        if (!IsAlive(player) || !player.activeInHierarchy || !IsAlive(player.transform)) return false;
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || player.scene.handle != scene.handle) return false;
        var visuals = PlayerFinder.playerActorVisuals;
        if (visuals is not null && (!IsAlive(visuals) || !visuals.isActiveAndEnabled || visuals.dead)) return false;
        long session = ((long)(int)SceneManager.GetActiveScene().handle << 32) | (uint)player.GetInstanceID();
        if (_session != session) { Reset(); _session = session; }
        if (!IsAlive(_camera) || !_camera.isActiveAndEnabled) _camera = Camera.main;
        if (!IsAlive(_camera) || !_camera.isActiveAndEnabled || !IsAlive(_camera.transform)) return false;
        _position = player.transform.position;
        _right = _camera.transform.right;
        _forward = _camera.transform.forward;
        if (!NormalizeXZ(ref _right) || !NormalizeXZ(ref _forward)) return false;
        HasWorld = true;
        _found = _preferred = false;
        _lootListeners.Clear();
        _selected = SelectedPointer();
        _lootRange = LootDetectionRange();

        var gold = GroundGoldVisuals.all;
        if (gold is not null && !gold.WasCollected)
            for (int i = 0; i < gold.Count; i++)
                try
                {
                    var item = gold[i];
                    if (!IsAlive(item)) continue;
                    _coins++;
                    RememberLoot(item.worldObjectClickListener);
                    // Gold is automatically collected; its optional label is not availability.
                    if (item.goldValue > 0)
                        Consider(item.worldObjectClickListener, item, item.label, WorldCueCategory.Coins);
                }
                catch (Exception ex) { LastFailure ??= ex.Message; }

        var equipment = GroundItemVisuals.all;
        if (equipment is not null && !equipment.WasCollected)
            for (int i = 0; i < equipment.Count; i++)
                try
                {
                    var item = equipment[i];
                    if (!IsAlive(item)) continue;
                    _equipment++;
                    RememberLoot(item.worldObjectClickListener);
                    var data = item.itemData;
                    if (data is null || data.WasCollected) { _missingItemData++; continue; }
                    if (!data.isEquipment()) { _nonEquipment++; continue; }
                    var label = item.label;
                    if (!IsAlive(label)) { _missingLabel++; continue; }
                    if (label.lastFilterResult == Rule.RuleOutcome.HIDE) { _filtered++; continue; }
                    Consider(item.worldObjectClickListener, item, label, WorldCueCategory.Equipment);
                }
                catch (Exception ex) { LastFailure ??= ex.Message; }

        var listeners = WorldObjectClickListener.allListener;
        if (listeners is not null && !listeners.WasCollected)
            for (int i = 0; i < listeners.Count; i++)
                try
                {
                    var listener = listeners[i];
                    if (!IsAlive(listener) || _lootListeners.Contains(listener.Pointer)) continue;
                    // Pooled/collected visuals can disappear before their listener leaves the other registry.
                    if (listener.GetComponent<GroundGoldVisuals>() is not null ||
                        listener.GetComponent<GroundItemVisuals>() is not null) continue;
                    if (NpcWorldProbe.IsNpc(listener)) continue;
                    _objects++;
                    Consider(listener, listener, null, WorldCueCategory.Interactable);
                }
                catch (Exception ex) { LastFailure ??= ex.Message; }

        foreach (var candidate in Targets)
        {
            if (!candidate.HasValue) continue;
            var value = candidate.Value;
            bool preferred = _categoryPreferred[(int)value.Key.Category];
            if (_found && (!preferred && _preferred || preferred == _preferred &&
                (value.Distance > _nearest.Distance || value.Distance == _nearest.Distance &&
                    value.Key.InstanceId >= _nearest.Key.InstanceId))) continue;
            _nearest = value;
            _preferred = preferred;
            _found = true;
        }
        for (int i = 0; i < _held.Length; i++) _held[i] = Targets[i]?.Key.InstanceId ?? 0;
        target = _nearest;
        return _found;
    }

    private void RememberLoot(WorldObjectClickListener listener)
    {
        if (IsAlive(listener)) _lootListeners.Add(listener.Pointer);
    }

    private void Consider(WorldObjectClickListener listener, MonoBehaviour owner,
        PickupableGroundLabel label, WorldCueCategory category)
    {
        bool interactable = category == WorldCueCategory.Interactable;
        if (interactable && (!IsAlive(listener) || !listener.isActiveAndEnabled || !IsAlive(listener.gameObject) ||
            !listener.gameObject.activeInHierarchy) || !owner.isActiveAndEnabled ||
            !IsAlive(owner.gameObject) || !owner.gameObject.activeInHierarchy || !IsAlive(owner.transform))
        { _unavailable++; return; }
        if (owner.gameObject.scene.handle != SceneManager.GetActiveScene().handle)
        { _unavailable++; return; }
        float range = interactable ? listener.interactionRange : _lootRange;
        if ((!float.IsFinite(range) || range <= 0) && IsAlive(listener)) range = listener.interactionRange;
        Vector3 offset = owner.transform.position - _position;
        if (IsAlive(listener) && listener.controllerTargetingUsePlayerY) offset.y = 0;
        float distance = offset.magnitude;
        int id = owner.GetInstanceID();
        int categoryIndex = (int)category;
        bool held = _held[categoryIndex] == id;
        NaturalCueKind? sound = null;
        bool soundRead = false;
        float audible = range;
        if (interactable && float.IsFinite(range) && range > 0 && LandmarkRange > range && float.IsFinite(distance) &&
            distance > WorldCueReach.Limit(range, held) && distance <= WorldCueReach.Limit(LandmarkRange, held))
        {
            sound = WorldCueIdentityReader.ReadKind(listener);
            soundRead = true;
            if (WorldCueReach.IsLandmark(sound)) audible = LandmarkRange;
        }
        if (!float.IsFinite(range) || range <= 0 || !float.IsFinite(distance) || distance > WorldCueReach.Limit(audible, held))
        { _outOfRange++; return; }
        bool preferred = _selected != IntPtr.Zero && (IsAlive(listener) && _selected == listener.Pointer || _selected == owner.Pointer ||
            IsAlive(label) && _selected == label.Pointer);
        var previous = Targets[categoryIndex];
        if (previous.HasValue && (!preferred && _categoryPreferred[categoryIndex] || preferred == _categoryPreferred[categoryIndex] &&
            (distance > previous.Value.Distance || distance == previous.Value.Distance && id >= previous.Value.Key.InstanceId))) return;
        if (interactable && !listener.CanInteract()) { _unavailable++; return; }
        float pan = 0, forward = 0;
        if (NormalizeXZ(ref offset))
        {
            pan = Math.Clamp(Vector3.Dot(offset, _right), -1, 1);
            forward = Math.Clamp(Vector3.Dot(offset, _forward), -1, 1);
        }
        if (interactable && !soundRead) sound = WorldCueIdentityReader.ReadKind(listener);
        float gain = WorldCueReach.IsLandmark(sound) ? WorldCueReach.Gain(distance, range, LandmarkRange) : 1;
        Targets[categoryIndex] = new(_session, new(category, id), distance, range, pan, forward, sound, gain);
        _categoryPreferred[categoryIndex] = preferred;
    }

    private static IntPtr SelectedPointer()
    {
        var input = EpochInputManager.instance;
        if (!IsAlive(input)) return IntPtr.Zero;
        var targeting = input.InteractableTargeting;
        if (targeting is null || targeting.WasCollected) return IntPtr.Zero;
        var target = targeting.ActiveTarget;
        if (target is null || target.WasCollected) return IntPtr.Zero;
        var item = target.item;
        return IsAlive(item) ? item.Pointer : IntPtr.Zero;
    }

    private static float LootDetectionRange()
    {
        var input = EpochInputManager.instance;
        if (!IsAlive(input)) return 0;
        var configuration = input.targetingConfiguration;
        // KeyBasedInteractableTargeting.UpdateTargets compares item distance to itemsRange.
        return configuration is not null && !configuration.WasCollected ? configuration.itemsRange : 0;
    }

    private static bool NormalizeXZ(ref Vector3 value)
    {
        double squared = (double)value.x * value.x + (double)value.z * value.z;
        if (!double.IsFinite(squared) || squared <= 0) return false;
        double length = Math.Sqrt(squared);
        value = new((float)(value.x / length), 0, (float)(value.z / length));
        return true;
    }

    internal void Reset()
    {
        _camera = null; _session = 0; _lootListeners.Clear(); LastFailure = null;
        HasWorld = false;
        Array.Clear(Targets, 0, Targets.Length);
        Array.Clear(_held, 0, _held.Length);
    }
}
