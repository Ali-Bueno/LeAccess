using System.Collections.Generic;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.WorldCues;

internal enum InteractableRole { None, Shrine, TimeRift, Nemesis, Cache }

// Interactables recognized by their game components; sound and fallback name follow the role.
internal static class InteractableRoles
{
    // Shrines, time rifts, the Nemesis altar and caches, by the game components on the listener or its owner;
    // shrines also by the objects their manager placed.
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
        return Available(listener.GetComponentInParent<OneShotCacheInteractableObject>())
            ? InteractableRole.Cache : InteractableRole.None;
    }

    private static bool Available(Behaviour component) => IsAlive(component) &&
        component.isActiveAndEnabled && IsAlive(component.gameObject) && component.gameObject.activeInHierarchy;

    internal static string NameKey(InteractableRole role) => role switch
    {
        InteractableRole.Shrine => "exploration.shrine",
        InteractableRole.TimeRift => "exploration.time_rift",
        InteractableRole.Nemesis => "exploration.nemesis",
        InteractableRole.Cache => "exploration.chest",
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
    internal static string Name(InteractableRole role, WorldObjectClickListener listener, HoverTagOnMouseOver tag)
    {
        if (role != InteractableRole.Shrine || !IsAlive(listener)) return Name(role, tag);
        return ShrineName(new[] { tag, listener.GetComponentInParent<HoverTagOnMouseOver>(true),
            listener.GetComponentInChildren<HoverTagOnMouseOver>(true) });
    }

    /// <summary>The first hover text the game gives any of the tags, else the mod's own word for a shrine.</summary>
    internal static string ShrineName(IEnumerable<HoverTagOnMouseOver> tags) =>
        GameShrineName(tags) ?? ModText.Get(NameKey(InteractableRole.Shrine));

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
