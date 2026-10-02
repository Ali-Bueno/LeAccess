using System;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Npcs;

internal static class NpcNameReader
{
    internal static string Read(WorldObjectClickListener listener, out string source)
    {
        string name = ReadName(listener, out source);
        string serviceKey = WorldServiceReader.ReadServiceKey(listener);
        if (serviceKey is null) return name;
        source = string.IsNullOrWhiteSpace(source) ? "native service" : source + " + native service";
        return AppendLabel(name, ModText.Get(serviceKey));
    }

    private static string ReadName(WorldObjectClickListener listener, out string source)
    {
        source = null;
        string name = ReadScope(listener, listener, ref source);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        var relay = listener._NPCinteraction;
        if (IsAlive(relay) && relay.gameObject != listener.gameObject)
        {
            name = ReadScope(relay, listener, ref source);
            if (!string.IsNullOrWhiteSpace(name)) return name;
        }
        var parentNpc = listener.GetComponentInParent<NPCInteraction>();
        if (IsAlive(parentNpc) && parentNpc.gameObject != listener.gameObject &&
            (!IsAlive(relay) || parentNpc.gameObject != relay.gameObject))
        {
            name = ReadScope(parentNpc, listener, ref source);
            if (!string.IsNullOrWhiteSpace(name)) return name;
        }
        var parentTooltip = listener.GetComponentInParent<NpcNameTooltip>();
        name = BelongsTo(parentTooltip, listener) ? ReadTooltip(parentTooltip, ref source) : null;
        if (!string.IsNullOrWhiteSpace(name)) return name;
        var parentDisplay = listener.GetComponentInParent<BaseDisplayInformation>();
        name = BelongsTo(parentDisplay, listener) ? ReadDisplay(parentDisplay, ref source) : null;
        if (!string.IsNullOrWhiteSpace(name)) return name;

        var conversation = listener._leConversationTrigger;
        if (IsAlive(conversation))
        {
            var conversant = conversation.conversant;
            // The conversation actor may be the player; only the explicit conversant belongs to this NPC.
            if (IsAlive(conversant) && (!IsAlive(PlayerFinder.player) || conversant.gameObject != PlayerFinder.player))
            {
                name = ReadScope(conversant, listener, ref source);
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
        }
        return NpcConversationNameReader.Read(listener, out source);
    }

    private static string ReadScope(Component root, WorldObjectClickListener listener, ref string source)
    {
        if (!IsAlive(root) || !root.gameObject.activeInHierarchy) return null;
        var tooltips = root.GetComponentsInChildren<NpcNameTooltip>(false);
        if (tooltips is not null && !tooltips.WasCollected)
            foreach (var tooltip in tooltips)
            {
                if (!BelongsTo(tooltip, listener)) continue;
                string name = ReadTooltip(tooltip, ref source);
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
        var displays = root.GetComponentsInChildren<BaseDisplayInformation>(false);
        if (displays is not null && !displays.WasCollected)
            foreach (var display in displays)
            {
                if (!BelongsTo(display, listener)) continue;
                string name = ReadDisplay(display, ref source);
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
        var actors = root.GetComponentsInChildren<OverrideActorName>(false);
        if (actors is not null && !actors.WasCollected)
            foreach (var actor in actors)
            {
                if (!BelongsTo(actor, listener)) continue;
                string name = TextUtils.CleanText(actor.GetOverrideName());
                if (string.IsNullOrWhiteSpace(name)) continue;
                source = "dialogue actor";
                return name;
            }
        return null;
    }

    private static bool BelongsTo(Component name, WorldObjectClickListener listener)
    {
        if (!IsAlive(name) || !name.gameObject.activeInHierarchy) return false;
        var owner = name.GetComponentInParent<WorldObjectClickListener>();
        return !IsAlive(owner) || owner.GetInstanceID() == listener.GetInstanceID();
    }

    private static string ReadTooltip(NpcNameTooltip tooltip, ref string source)
    {
        if (!IsAlive(tooltip) || !tooltip.gameObject.activeInHierarchy) return null;
        string name = TextUtils.CleanText(string.IsNullOrWhiteSpace(tooltip.TopTextKey)
            ? tooltip.TopText : Localization.GetText(tooltip.TopTextKey, tooltip.TopText));
        if (!string.IsNullOrWhiteSpace(name)) source = "NPC tooltip";
        if (string.IsNullOrWhiteSpace(name)) return null;
        string subtitle = TextUtils.CleanText(string.IsNullOrWhiteSpace(tooltip.BottomTextKey)
            ? tooltip.BottomText : Localization.GetText(tooltip.BottomTextKey, tooltip.BottomText));
        return AppendLabel(name, subtitle);
    }

    private static string AppendLabel(string name, string detail)
    {
        if (string.IsNullOrWhiteSpace(name)) return detail;
        if (string.IsNullOrWhiteSpace(detail) || string.Equals(name, detail, StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(". " + detail, StringComparison.OrdinalIgnoreCase)) return name;
        return name + ". " + detail;
    }

    private static string ReadDisplay(BaseDisplayInformation display, ref string source)
    {
        if (!IsAlive(display) || !display.gameObject.activeInHierarchy) return null;
        string name = TextUtils.CleanText(display.GetLocalizedName());
        if (!string.IsNullOrWhiteSpace(name)) source = "actor display";
        return name;
    }
}
