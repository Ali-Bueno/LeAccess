using System;
using System.Collections.Generic;
using ItemFiltering;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Npcs;
using UnityEngine;
using UnityEngine.SceneManagement;
using LeAccess.Accessibility.Players;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.WorldCues;

internal sealed class WorldCueProbe
{
    private readonly HashSet<IntPtr> _lootListeners = new();
    // An item's type and rarity never change; pooled visuals get new data, hence the pair as key.
    private readonly Dictionary<(int, IntPtr), NaturalCueKind> _lootKinds = new();
    private Camera _camera;
    private long _session;
    private Vector3 _position, _right, _forward;
    private IntPtr _selected;
    private bool _found, _preferred;
    private WorldCueTarget _nearest;
    private float _lootRange, _loopReach, _landmarkReach;
    private readonly bool[] _categoryPreferred = new bool[3];
    private readonly int[] _held = new int[3];
    /// <summary>Group audible radius in metres for exits, waypoints, the stash and shrines (a kind's own range replaces it); at most the native radius disables it.</summary>
    internal float LandmarkRange { get; set; }
    /// <summary>Kinds the loop mixer can play; null keeps every object on the repeated cue.</summary>
    internal Func<NaturalCueKind, bool> Loopable { get; set; }
    /// <summary>Group audible radius in metres for looping objects (a kind's own range replaces it).</summary>
    internal float LoopRange { get; set; }
    /// <summary>Every eligible object with a playable loop, in registry order.</summary>
    internal List<LoopCandidate> Loops { get; } = new();
    internal WorldCueTarget?[] Targets { get; } = new WorldCueTarget?[3];
    private int _coins, _equipment, _objects, _outOfRange, _filtered, _unavailable;
    private int _missingItemData, _nonEquipment, _missingLabel, _pickups;
    internal string Diagnostics => $"registry={_coins}/{_equipment}/{_objects}/{_pickups} outOfRange={_outOfRange} filtered={_filtered} unavailable={_unavailable} missingData={_missingItemData} nonEquipment={_nonEquipment} missingLabel={_missingLabel}";
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
        Loops.Clear();
        _coins = _equipment = _objects = _outOfRange = _filtered = _unavailable = 0;
        _missingItemData = _nonEquipment = _missingLabel = _pickups = 0;
        var player = LocalPlayerSource.Player;
        if (!IsAlive(player) || !player.activeInHierarchy || !IsAlive(player.transform)) return false;
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || player.scene.handle != scene.handle) return false;
        var visuals = LocalPlayerSource.Visuals;
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
        // Objects are collected out to the farthest audible kind; selection applies each kind's own range.
        _loopReach = Loopable == null ? 0 : SoundLevels.LargestRange(LoopRange, kind => Loopable(kind));
        _landmarkReach = SoundLevels.LargestRange(LandmarkRange, kind => WorldCueReach.IsLandmark(kind));

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
                    var label = item.label;
                    if (!IsAlive(label)) { _missingLabel++; continue; }
                    if (label.lastFilterResult == Rule.RuleOutcome.HIDE) { _filtered++; continue; }
                    var lootKey = (item.GetInstanceID(), data.Pointer);
                    if (!_lootKinds.TryGetValue(lootKey, out var kind)) _lootKinds[lootKey] = kind = GroundLootClassifier.Classify(data);
                    if (GroundLootClassifier.IsGear(kind)) Consider(item.worldObjectClickListener, item, label, WorldCueCategory.Equipment, kind);
                    else { _nonEquipment++; ConsiderPickup(item.worldObjectClickListener, item, kind); }
                }
                catch (Exception ex) { LastFailure ??= ex.Message; }

        // Potions and tomes have their own native registries and listeners, like gold.
        var potions = GroundPotionVisuals.all;
        if (potions is not null && !potions.WasCollected)
            for (int i = 0; i < potions.Count; i++)
                try { var item = potions[i]; if (IsAlive(item)) ConsiderPickup(item.worldObjectClickListener, item, NaturalCueKind.Potion); }
                catch (Exception ex) { LastFailure ??= ex.Message; }
        var experienceTomes = GroundXPTomeVisuals.all;
        if (experienceTomes is not null && !experienceTomes.WasCollected)
            for (int i = 0; i < experienceTomes.Count; i++)
                try { var item = experienceTomes[i]; if (IsAlive(item)) ConsiderPickup(item.worldObjectClickListener, item, NaturalCueKind.Tome); }
                catch (Exception ex) { LastFailure ??= ex.Message; }
        var favorTomes = GroundFavorTomeVisuals.all;
        if (favorTomes is not null && !favorTomes.WasCollected)
            for (int i = 0; i < favorTomes.Count; i++)
                try { var item = favorTomes[i]; if (IsAlive(item)) ConsiderPickup(item.worldObjectClickListener, item, NaturalCueKind.Tome); }
                catch (Exception ex) { LastFailure ??= ex.Message; }

        var listeners = WorldObjectClickListener.allListener;
        if (listeners is not null && !listeners.WasCollected)
            for (int i = 0; i < listeners.Count; i++)
                try
                {
                    var listener = listeners[i];
                    if (!IsAlive(listener) || _lootListeners.Contains(listener.Pointer)) continue;
                    // Pooled/collected visuals can disappear before their listener leaves the other registry.
                    if (GroundLootListener.Owns(listener)) continue;
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
        PickupableGroundLabel label, WorldCueCategory category, NaturalCueKind? lootKind = null)
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
        float reach = Math.Max(range, Math.Max(_landmarkReach, _loopReach));
        if (!float.IsFinite(range) || range <= 0 || !float.IsFinite(distance)) { _outOfRange++; return; }
        // What a sighted player sees on screen is audible even past the range (loops and landmarks only, below).
        bool screenKnown = distance > WorldCueReach.Limit(reach, held), onScreen = screenKnown && IsOnScreen(owner);
        if (screenKnown && !onScreen) { _outOfRange++; return; }
        NaturalCueKind? sound = category switch
        {
            WorldCueCategory.Coins => NaturalCueKind.Gold,
            WorldCueCategory.Equipment => lootKind ?? NaturalCueKind.Equipment,
            _ => null
        };
        if (interactable)
        {
            if (!listener.CanInteract()) { _unavailable++; return; }
            sound = WorldCueIdentityReader.ReadKind(listener);
        }
        // A silenced kind takes no slot, so the nearest audible object of its category still sounds.
        float level = sound.HasValue ? SoundLevels.Volume(sound.Value) : 1;
        if (!(level > 0)) { _filtered++; return; }
        Place(offset, out float pan, out float forward, out float front);
        // An object with a whole D2R loop belongs to the loop mixer, never also to the repeated cue.
        if (sound.HasValue && Loopable?.Invoke(sound.Value) == true)
        {
            if (!screenKnown) onScreen = IsOnScreen(owner);
            float kindRange = SoundLevels.Range(sound.Value, LoopRange);
            if (kindRange > 0 && (distance <= kindRange || onScreen))
                Loops.Add(new LoopCandidate(id, sound.Value, distance, pan, front, onScreen));
            else _outOfRange++;
            return;
        }
        // Loot cues keep their D2R recording only when the mixer cannot play it.
        if (!interactable) sound = null;
        float landmarkRange = WorldCueReach.IsLandmark(sound) ? SoundLevels.Range(sound.Value, LandmarkRange) : 0;
        float audible = Math.Max(range, landmarkRange);
        if (distance > WorldCueReach.Limit(audible, held) && !(WorldCueReach.IsLandmark(sound) && (onScreen || IsOnScreen(owner))))
        { _outOfRange++; return; }
        bool preferred = _selected != IntPtr.Zero && (IsAlive(listener) && _selected == listener.Pointer || _selected == owner.Pointer ||
            IsAlive(label) && _selected == label.Pointer);
        var previous = Targets[categoryIndex];
        if (previous.HasValue && (!preferred && _categoryPreferred[categoryIndex] || preferred == _categoryPreferred[categoryIndex] &&
            (distance > previous.Value.Distance || distance == previous.Value.Distance && id >= previous.Value.Key.InstanceId))) return;
        float gain = (WorldCueReach.IsLandmark(sound) ? WorldCueReach.Gain(distance, range, landmarkRange) : 1) * level;
        Targets[categoryIndex] = new(_session, new(category, id), distance, range, pan, forward, sound, gain);
        _categoryPreferred[categoryIndex] = preferred;
    }

    // Pickups without a repeated cue sound only through the loop mixer.
    private void ConsiderPickup(WorldObjectClickListener listener, MonoBehaviour owner, NaturalCueKind kind)
    {
        _pickups++;
        RememberLoot(listener);
        if (Loopable?.Invoke(kind) != true || !(SoundLevels.Volume(kind) > 0)) return;
        if (!owner.isActiveAndEnabled || !IsAlive(owner.gameObject) || !owner.gameObject.activeInHierarchy ||
            !IsAlive(owner.transform) || owner.gameObject.scene.handle != SceneManager.GetActiveScene().handle)
        { _unavailable++; return; }
        Vector3 offset = owner.transform.position - _position;
        if (IsAlive(listener) && listener.controllerTargetingUsePlayerY) offset.y = 0;
        float distance = offset.magnitude;
        if (!float.IsFinite(distance)) { _outOfRange++; return; }
        bool onScreen = IsOnScreen(owner);
        float kindRange = SoundLevels.Range(kind, LoopRange);
        if (!(kindRange > 0) || distance > kindRange && !onScreen) { _outOfRange++; return; }
        Place(offset, out float pan, out _, out float front);
        Loops.Add(new LoopCandidate(owner.GetInstanceID(), kind, distance, pan, front, onScreen));
    }

    private bool IsOnScreen(MonoBehaviour owner) => BossLifeProbe.OnScreen(_camera, owner.transform.position);

    // Camera-relative pan and forward axis, plus metres ahead for the loop's rear pitch blend.
    private void Place(Vector3 offset, out float pan, out float forward, out float front)
    {
        pan = forward = 0;
        front = offset.x * _forward.x + offset.z * _forward.z;
        if (!NormalizeXZ(ref offset)) return;
        pan = Math.Clamp(Vector3.Dot(offset, _right), -1, 1);
        forward = Math.Clamp(Vector3.Dot(offset, _forward), -1, 1);
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
        _camera = null; _session = 0; _lootListeners.Clear(); _lootKinds.Clear(); LastFailure = null;
        HasWorld = false;
        Array.Clear(Targets, 0, Targets.Length);
        Array.Clear(_held, 0, _held.Length);
    }
}
