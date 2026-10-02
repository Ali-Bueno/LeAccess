using System;
using LE.Dialog;
using LeAccess.Accessibility.Utils;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Npcs;

internal static class NpcConversationNameReader
{
    internal static string Read(WorldObjectClickListener listener, out string source)
    {
        source = null;
        string name = null;
        int databaseId = 0, actorId = 0;
        bool uncertain = false;

        void Consider(LEConversationTrigger trigger)
        {
            if (!Owned(trigger, listener) || !trigger.isActiveAndEnabled) return;
            string candidate = ReadTrigger(trigger, listener, out int candidateDatabase, out int candidateActor);
            if (string.IsNullOrWhiteSpace(candidate)) { uncertain = true; return; }
            if (name is not null && (databaseId != candidateDatabase || actorId != candidateActor ||
                !string.Equals(name, candidate, StringComparison.Ordinal)))
            { uncertain = true; return; }
            name = candidate;
            databaseId = candidateDatabase;
            actorId = candidateActor;
        }

        var direct = listener._leConversationTrigger;
        if (!IsAlive(direct)) direct = listener.GetComponent<LEConversationTrigger>();
        Consider(direct);

        var relay = listener._NPCinteraction;
        if (!IsAlive(relay)) relay = listener.GetComponent<RelayNPCInteraction>();
        if (Owned(relay, listener) && relay.isActiveAndEnabled)
        {
            var interactions = relay.otherInteractables;
            if (interactions is not null && !interactions.WasCollected)
                foreach (var interaction in interactions)
                {
                    if (!Owned(interaction, listener) || !interaction.isActiveAndEnabled) continue;
                    // Z22 relays explicitly target this component's GO; unrelated siblings are not name sources.
                    Consider(interaction.GetComponent<LEConversationTrigger>());
                }
        }
        if (uncertain || name is null) return null;
        source = "dialogue database";
        return name;
    }

    private static string ReadTrigger(LEConversationTrigger trigger, WorldObjectClickListener listener,
        out int databaseId, out int actorId)
    {
        databaseId = actorId = 0;
        var database = trigger.ExternalDb;
        if (!trigger.useExternalDb)
        {
            // Use the native Start database route without invoking a singleton-creating getter.
            var manager = PixelCrushers.DialogueSystem.DialogueManager.instance;
            database = IsAlive(manager) ? manager.initialDatabase : null;
        }
        if (!IsAlive(database)) return null;
        var conversation = database.GetConversation(trigger.ConversationID);
        if (conversation is null || conversation.WasCollected) return null;
        var actor = database.GetActor(conversation.ConversantID);
        if (actor is null || actor.WasCollected || actor.IsPlayer) return null;
        var conversant = trigger.conversant;
        if (conversant is not null && conversant.WasCollected) return null;
        // ConversationStarter selects the explicit conversant, then its own transform (reference/npc-names.md).
        if (!IsAlive(conversant)) conversant = trigger.transform;
        if (!Owned(conversant, listener) || IsAlive(PlayerFinder.player) && conversant.gameObject == PlayerFinder.player)
            return null;
        string displayName = OverrideActorName.GetActorName(conversant);
        if (string.IsNullOrWhiteSpace(displayName)) return null;
        // GetActorName returns the internal identifier on lookup failure; require a real localized entry.
        string key = "Actor_Name_" + displayName;
        if (!Localization.TryGetText(key, out string localized)) return null;
        string name = TextUtils.CleanText(localized);
        if (string.IsNullOrWhiteSpace(name) || string.Equals(name, key, StringComparison.Ordinal)) return null;
        databaseId = database.GetInstanceID();
        actorId = conversation.ConversantID;
        return name;
    }

    private static bool Owned(Component component, WorldObjectClickListener listener)
    {
        if (!IsAlive(component) || !component.gameObject.activeInHierarchy) return false;
        var owner = component.GetComponentInParent<WorldObjectClickListener>();
        return !IsAlive(owner) || owner.GetInstanceID() == listener.GetInstanceID();
    }
}
