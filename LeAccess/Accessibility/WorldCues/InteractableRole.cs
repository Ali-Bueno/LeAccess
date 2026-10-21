using System;
using System.Collections.Generic;
using LE.Gameplay.Monolith;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.WorldCues;

internal enum InteractableRole
{
    None, Shrine, TimeRift, Nemesis, Cache, MorditasCrystal, MorditasReward, UnstableRift, Beacon, MonolithForge,
    TimelineCatalyst, OmenMirror
}

// Interactables recognized by their game components; sound and fallback name follow the role.
internal static class InteractableRoles
{
    // Shrines, rifts, the Nemesis altar, caches and the echo and Morditas objects, by the game components on the
    // listener or its owner; shrines also by the objects their manager placed.
    internal static InteractableRole Read(WorldObjectClickListener listener)
    {
        if (!Available(listener)) return InteractableRole.None;
        if (Available(listener.GetComponentInParent<Shrine>()) || Available(listener.GetComponentInParent<ShrineSync>()) ||
            Available(listener.GetComponentInParent<ShrineVisualsCreator>()) || ShrineObjects.Contains(listener))
            return InteractableRole.Shrine;
        if (Available(listener.GetComponentInParent<NemesisEncounterInteractableObject>())) return InteractableRole.Nemesis;
        if (Available(listener.GetComponentInParent<TimeRiftInteraction>()) ||
            Available(listener.GetComponentInParent<ActivateTimeRift>()) ||
            Available(listener.GetComponentInParent<TimeBeastRift>()) ||
            Available(listener.GetComponentInParent<MonolithPortal>()) ||
            Available(listener.GetComponentInParent<ShatteredRoadPortalInteractable>())) return InteractableRole.TimeRift;
        if (Available(listener.GetComponentInParent<OneShotCacheInteractableObject>())) return InteractableRole.Cache;
        if (ShrineObjectName(listener) is not null) return InteractableRole.Shrine;
        return ReadEventObject(listener);
    }

    // Shrine prefabs are also named "<Effect> Shrine (Clone)"; the listener may sit on a child of that object.
    private const int ShrineNameDepth = 4;

    private static string ShrineObjectName(WorldObjectClickListener listener)
    {
        var current = listener.gameObject;
        for (int depth = 0; depth < ShrineNameDepth && IsAlive(current); depth++)
        {
            if (InteractableNames.IsShrineObject(current.name)) return current.name;
            var parent = current.transform.parent;
            current = parent is null ? null : parent.gameObject;
        }
        return null;
    }

    // Owners of the event roles by listener: the scanner asks for every listener twice a second, and an active
    // listener's ancestors (so their components) stay the same for its life.
    private static readonly Dictionary<(int, IntPtr), (InteractableRole Role, Behaviour Owner)> EventOwners = new();
    // A scene holds a few hundred listeners; past this the cache restarts instead of growing over a long session.
    private const int MaxCachedListeners = 4096;

    // Objects of the endgame events: Rage of Morditas, monolith echoes and omen windows.
    private static InteractableRole ReadEventObject(WorldObjectClickListener listener)
    {
        var key = (listener.GetInstanceID(), listener.Pointer);
        if (!EventOwners.TryGetValue(key, out var owner))
        {
            if (EventOwners.Count >= MaxCachedListeners) EventOwners.Clear();
            EventOwners[key] = owner = FindEventOwner(listener);
        }
        return owner.Role != InteractableRole.None && Available(owner.Owner) ? owner.Role : InteractableRole.None;
    }

    private static (InteractableRole, Behaviour) FindEventOwner(WorldObjectClickListener listener)
    {
        Behaviour owner;
        if (IsAlive(owner = listener.GetComponentInParent<RageOfMorditasInteractableObject>())) return (InteractableRole.MorditasCrystal, owner);
        if (IsAlive(owner = listener.GetComponentInParent<RageOfMorditasRewardVisualController>())) return (InteractableRole.MorditasReward, owner);
        if (IsAlive(owner = listener.GetComponentInParent<UnstableRiftVisualController>())) return (InteractableRole.UnstableRift, owner);
        if (IsAlive(owner = listener.GetComponentInParent<BeaconVisualsController>())) return (InteractableRole.Beacon, owner);
        if (IsAlive(owner = listener.GetComponentInParent<ForgeVisualController>())) return (InteractableRole.MonolithForge, owner);
        if (IsAlive(owner = listener.GetComponentInParent<TimelineCatalystVisualController>())) return (InteractableRole.TimelineCatalyst, owner);
        if (IsAlive(owner = listener.GetComponentInParent<OmenMirrorVisualController>())) return (InteractableRole.OmenMirror, owner);
        return (InteractableRole.None, null);
    }

    /// <summary>Roles that lead somewhere or start the next stretch of an echo; they are filed and sound as portals.</summary>
    internal static bool IsTravel(InteractableRole role) => role is InteractableRole.TimeRift or InteractableRole.UnstableRift or
        InteractableRole.Beacon or InteractableRole.TimelineCatalyst;

    private static bool Available(Behaviour component) => IsAlive(component) &&
        component.isActiveAndEnabled && IsAlive(component.gameObject) && component.gameObject.activeInHierarchy;

    internal static string NameKey(InteractableRole role) => role switch
    {
        InteractableRole.Shrine => "exploration.shrine",
        InteractableRole.TimeRift => "exploration.time_rift",
        InteractableRole.Nemesis => "exploration.nemesis",
        InteractableRole.Cache => "exploration.chest",
        InteractableRole.MorditasCrystal => "exploration.morditas_rage",
        InteractableRole.MorditasReward => "exploration.morditas_reward",
        InteractableRole.UnstableRift => "exploration.unstable_rift",
        InteractableRole.Beacon => "exploration.beacon",
        InteractableRole.MonolithForge => "service.forge",
        InteractableRole.TimelineCatalyst => "exploration.timeline_catalyst",
        InteractableRole.OmenMirror => "exploration.omen_mirror",
        _ => null
    };

    /// <summary>The game's hover text, localized when it carries a key; the mod's own name only when the game gives none.</summary>
    internal static string Name(InteractableRole role, HoverTagOnMouseOver tag)
    {
        string game = IsAlive(tag) ? GameName(tag) : null;
        if (!string.IsNullOrWhiteSpace(game)) return game;
        string key = NameKey(role);
        return key is null ? null : ModText.Get(key);
    }

    /// <summary>The name for a listener: shrines also search its ancestors and descendants for the hover tag, enabled or not.</summary>
    internal static string Name(InteractableRole role, WorldObjectClickListener listener, HoverTagOnMouseOver tag,
        Func<string, string> shrineBuffName = null)
    {
        if (!IsAlive(listener)) return Name(role, tag);
        if (role == InteractableRole.Shrine)
        {
            string objectName = ShrineObjectName(listener);
            return GameShrineName(new[] { tag, listener.GetComponentInParent<HoverTagOnMouseOver>(true),
                listener.GetComponentInChildren<HoverTagOnMouseOver>(true) }) ??
                (objectName is null ? null : shrineBuffName?.Invoke(objectName)) ?? ModText.Get(NameKey(InteractableRole.Shrine));
        }
        var label = InteractLabel(role, listener);
        return label is null ? Name(role, tag) : GameShrineName(new[] { tag, label }) ?? ModText.Get(NameKey(role));
    }

    // The Morditas crystals name themselves through their own hover label.
    private static HoverTagOnMouseOver InteractLabel(InteractableRole role, WorldObjectClickListener listener) => role switch
    {
        InteractableRole.MorditasCrystal => listener.GetComponentInParent<RageOfMorditasInteractableObject>()?.InteractLabel,
        InteractableRole.MorditasReward => listener.GetComponentInParent<RageOfMorditasRewardVisualController>()?.InteractLabel,
        _ => null
    };

    /// <summary>The game's own shrine name from its tags, or null when none carries one.</summary>
    internal static string GameShrineName(IEnumerable<HoverTagOnMouseOver> tags)
    {
        foreach (var tag in tags)
        {
            string game = IsAlive(tag) ? GameName(tag) : null;
            if (!string.IsNullOrWhiteSpace(game)) return game;
        }
        return null;
    }

    private static string GameName(HoverTagOnMouseOver tag)
    {
        string key = tag.localizationKey;
        string text = string.IsNullOrWhiteSpace(key) ? tag.hoverText : Localization.GetText(key, tag.hoverText);
        // An unresolved key comes back as itself.
        return text == key ? null : LeAccess.Accessibility.Utils.TextUtils.CleanText(text);
    }
}
