using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.WorldCues;
using Networking.Multiplayer.Interactables.Portals;
using UnityEngine;

internal static class NaturalIdentityTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        T Attach<T>(WorldObjectClickListener listener, T component) where T : Behaviour
        { component.gameObject = listener.gameObject; listener.gameObject.Components.Add(component); return component; }

        var listener = new WorldObjectClickListener();
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "An unclassified interaction never claims chest identity");
        listener.ServiceKey = "service.stash";
        Check(WorldCueIdentityReader.ReadKind(listener) == NaturalCueKind.Stash, "Verified native stash selects the stash recording");
        listener.ServiceKey = "service.waypoint";
        Check(WorldCueIdentityReader.ReadKind(listener) == NaturalCueKind.Portal, "Verified waypoint selects the portal recording");
        listener.ServiceKey = "service.merchant";
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "Merchant service is not disguised as a stash or chest");
        listener.ServiceKey = null;
        var portal = new PortalVisualController { mouseListenerObj = listener.gameObject };
        listener.gameObject.Parent = portal.gameObject;
        Check(WorldCueIdentityReader.ReadKind(listener) == NaturalCueKind.Portal, "Owned parent portal selects its recording");
        portal.mouseListenerObj = new();
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "A sibling portal listener is not accepted");
        portal.mouseListenerObj = listener.gameObject; portal.Blocked = true;
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "A non-interactable portal supplies no portal identity");
        portal.Blocked = false; portal.WasCollected = true;
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "Collected native portal wrappers are safe");
        listener.gameObject.Parent = null;

        var tag = Attach(listener, new HoverTagOnMouseOver
        {
            hoverTargetType = HoverTagOnMouseOver.HoverTargetType.ZONE_TRANSITION,
            sceneInteraction = new() { sceneName = "Z12", isActiveAndEnabled = false }
        });
        listener.hoverTagOnMouseOver = tag;
        Check(WorldCueIdentityReader.ReadKind(listener) == NaturalCueKind.ZoneTransition,
            "Native transition type and same-scene destination metadata establish a transition sound");
        tag.sceneInteraction.gameObject.scene = new(2);
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "Foreign scene metadata cannot identify a transition");
        tag.sceneInteraction.gameObject.scene = new(1); tag.sceneInteraction.sceneName = " ";
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "An empty destination is not a verified transition");
        tag.hoverTargetType = HoverTagOnMouseOver.HoverTargetType.DUNGEON_TRANSITION;
        tag.dungeonTransitionInteraction = new() { sceneName = "Dungeon" };
        Check(WorldCueIdentityReader.ReadKind(listener) == NaturalCueKind.ZoneTransition, "Dungeon transition uses its own native destination");
        tag.gameObject = new();
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "An unrelated hover tag cannot label this object");
        tag.gameObject = listener.gameObject; tag.hoverTargetType = HoverTagOnMouseOver.HoverTargetType.OTHER;
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "A generic hover tag does not prove a portal or chest");

        var vault = Attach(listener, new VaultsOfUncertainFateChest());
        Check(WorldCueIdentityReader.ReadKind(listener) == NaturalCueKind.Chest, "An unopened native vault chest selects the chest recording");
        vault.hasBeenOpened = true;
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "An already opened vault cannot retain a chest cue");
        var monolith = Attach(listener, new LE.Gameplay.Monolith.MonolithChestInteraction());
        Check(WorldCueIdentityReader.ReadKind(listener) == NaturalCueKind.Chest, "An exact Monolith chest interaction establishes chest identity");
        monolith.isActiveAndEnabled = false;
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "Disabled chest interactions do not identify available chests");
        Attach(listener, new LE.Gameplay.Tombs.TombRewardChestInteraction());
        Check(WorldCueIdentityReader.ReadKind(listener) == NaturalCueKind.Chest, "Exact Tomb chest interaction is recognized");
        listener.isActiveAndEnabled = false;
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "Inactive listener cannot produce a natural object identity");

        WorldCueCategory category = WorldCueCategory.Interactable;
        foreach (var kind in new[] { NaturalCueKind.Stash, NaturalCueKind.Portal, NaturalCueKind.ZoneTransition, NaturalCueKind.Chest })
        {
            Check(WorldCueSynthesizer.Synthesize(category, -0.4f, -0.5f, 0.8f, kind)
                .SequenceEqual(NaturalCueBank.Build(kind, -0.4f, -0.5f, 0.8f)), $"{kind} rendering retains classified sample identity");
            Check(WorldCueSynthesizer.Duration(category, kind) == NaturalCueBank.Duration(kind), $"{kind} reserves its actual sample duration");
        }
        Check(!WorldCueSynthesizer.Synthesize(category, 0, 1, 1)
            .SequenceEqual(NaturalCueBank.Build(NaturalCueKind.Chest, 0, 1, 1)), "Unclassified interactables preserve the existing neutral prompt");
        Check(WorldCueSynthesizer.Synthesize(category, 0, 1, 1, (NaturalCueKind)99)
            .SequenceEqual(WorldCueSynthesizer.Synthesize(category, 0, 1, 1)), "Unavailable natural profiles fall back without losing the cue");
        return checks;
    }
}
