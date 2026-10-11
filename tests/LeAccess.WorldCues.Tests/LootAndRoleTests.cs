using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.WorldCues;
using UnityEngine;

internal static class LootAndRoleTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        NaturalCueKind Kind(LootTraits traits) => GroundLootClassifier.Classify(traits);

        var gear = new LootTraits(true, false, false, false, false, false, false, false, false);
        Check(Kind(gear) == NaturalCueKind.Equipment, "Normal equipment keeps the existing equipment loop");
        Check(Kind(gear with { Magic = true }) == NaturalCueKind.MagicEquipment, "Magic equipment");
        Check(Kind(gear with { Rare = true }) == NaturalCueKind.RareEquipment, "Rare equipment");
        Check(Kind(gear with { Exalted = true, Magic = true }) == NaturalCueKind.RareEquipment, "Exalted equipment sounds as rare");
        Check(Kind(gear with { UniqueSetOrLegendary = true }) == NaturalCueKind.LegendaryEquipment, "Unique, set and legendary equipment");
        Check(Kind(gear with { UniqueSetOrLegendary = true, Rare = true, Magic = true }) == NaturalCueKind.LegendaryEquipment,
            "The highest rarity wins when several predicates hold");
        var idol = new LootTraits(false, true, false, false, false, false, false, false, false);
        Check(Kind(idol) == NaturalCueKind.Equipment && Kind(idol with { Rare = true }) == NaturalCueKind.RareEquipment &&
            Kind(idol with { UniqueSetOrLegendary = true }) == NaturalCueKind.LegendaryEquipment, "Idols sound by rarity like equipment");
        var other = new LootTraits(false, false, false, false, false, false, false, false, false);
        Check(Kind(other with { Rune = true }) == NaturalCueKind.Rune, "Runes");
        Check(Kind(other with { Glyph = true }) == NaturalCueKind.Gem, "Glyphs sound as gems");
        Check(Kind(other with { AffixShard = true }) == NaturalCueKind.Scroll, "Affix shards sound as scrolls");
        Check(Kind(other) == NaturalCueKind.QuestItem && Kind(other with { Rare = true }) == NaturalCueKind.QuestItem,
            "Keys, lenses and other materials are quest items whatever their rarity");
        Check(Kind(other with { Rune = true, Glyph = true }) == NaturalCueKind.Rune, "A rune is never a glyph");
        Check(new[] { NaturalCueKind.Equipment, NaturalCueKind.MagicEquipment, NaturalCueKind.RareEquipment,
            NaturalCueKind.LegendaryEquipment }.All(GroundLootClassifier.IsGear) &&
            !GroundLootClassifier.IsGear(NaturalCueKind.Rune) && !GroundLootClassifier.IsGear(NaturalCueKind.QuestItem),
            "Gear is exactly the four equipment kinds");
        var data = new ItemData { Equipment = false, Rune = true };
        Check(GroundLootClassifier.Classify(data) == NaturalCueKind.Rune, "The classifier reads the game's predicates");

        // Probe: loot kinds reach the loop mixer; the filter still silences hidden labels.
        GroundItemVisuals.all = new(); WorldObjectClickListener.allListener = new();
        GroundGoldVisuals.all = new(); GroundPotionVisuals.all = new(); GroundXPTomeVisuals.all = new(); GroundFavorTomeVisuals.all = new();
        PlayerFinder.player = new(); PlayerFinder.playerActorVisuals = new(); Camera.main = new(); EpochInputManager.instance = new();
        var probe = new WorldCueProbe { Loopable = _ => true, LoopRange = 12, LandmarkRange = 12 };
        GroundItemVisuals Drop(float z, Action<ItemData> traits)
        {
            var value = new GroundItemVisuals(); value.transform.position = new(0, 0, z);
            value.worldObjectClickListener.gameObject = value.gameObject; traits(value.itemData);
            GroundItemVisuals.all.Add(value); WorldObjectClickListener.allListener.Add(value.worldObjectClickListener);
            return value;
        }
        var rare = Drop(2, d => d.Rare = true);
        var rune = Drop(3, d => { d.Equipment = false; d.Rune = true; });
        var key = Drop(4, d => d.Equipment = false);
        var hidden = Drop(5, d => { d.Equipment = false; d.Glyph = true; });
        hidden.label.lastFilterResult = ItemFiltering.Rule.RuleOutcome.HIDE;
        Check(!probe.TryCapture(out _) && probe.Loops.Count == 3 &&
            probe.Loops.Any(l => l.Kind == NaturalCueKind.RareEquipment && Math.Abs(l.Distance - 2) < 1e-4) &&
            probe.Loops.Any(l => l.Kind == NaturalCueKind.Rune) && probe.Loops.Any(l => l.Kind == NaturalCueKind.QuestItem),
            "Rarity equipment, runes and other loot loop by kind; filtered loot stays silent");
        probe.Loopable = kind => kind != NaturalCueKind.Rune && kind != NaturalCueKind.QuestItem;
        Check(!probe.TryCapture(out _) && probe.Loops.Count == 1 && probe.Loops[0].Kind == NaturalCueKind.RareEquipment &&
            probe.Targets.All(t => t == null), "Loot without a playable loop stays silent instead of borrowing the equipment cue");
        probe.Loopable = _ => true; probe.LoopRange = 3.5f;
        Check(!probe.TryCapture(out _) && probe.Loops.Count == 2, "Non-equipment loot respects the loop range");
        rune.itemData.WasCollected = true;
        Check(!probe.TryCapture(out _) && probe.Loops.Count == 1, "Collected item data is skipped");
        Check(hidden.itemData.Classified == 0 && rare.itemData.Classified == 1,
            "Filtered loot is never classified and a kind is read once per item");
        rare.itemData = new ItemData { Magic = true };
        Check(!probe.TryCapture(out _) && probe.Loops.Any(l => l.Kind == NaturalCueKind.MagicEquipment),
            "A pooled visual showing new item data is classified again");

        // Interactable identities.
        var listener = new WorldObjectClickListener();
        Behaviour Own(Behaviour component, WorldObjectClickListener owner = null)
        { owner ??= listener; component.gameObject = owner.gameObject; owner.gameObject.Components.Add(component); return component; }
        Check(InteractableRoles.Read(listener) == InteractableRole.None, "A plain interactable has no role");
        var shrine = Own(new Shrine());
        Check(WorldCueIdentityReader.ReadKind(listener) == NaturalCueKind.Shrine, "A shrine component selects the shrine loop");
        shrine.isActiveAndEnabled = false;
        Check(WorldCueIdentityReader.ReadKind(listener) == null, "A disabled shrine has no identity");
        Own(new ShrineSync());
        Check(InteractableRoles.Read(listener) == InteractableRole.Shrine, "ShrineSync also identifies a shrine");
        var visualShrine = new WorldObjectClickListener();
        var shrineRoot = new ShrineVisualsCreator();
        visualShrine.gameObject.Parent = shrineRoot.gameObject;
        Check(WorldCueIdentityReader.ReadKind(visualShrine) == NaturalCueKind.Shrine, "The shrine's visuals creator identifies its listener");

        // Shrines placed by the scene's manager, whatever component the listener carries.
        ShrineObjects.Reset();
        UnityEngine.SceneManagement.SceneManager.Names[1] = "Z10";
        var manager = new ShrinesManager();
        ShrinesManager.Scenes["Z10"] = manager;
        var placed = new UnityEngine.GameObject();
        var inside = new WorldObjectClickListener(); inside.gameObject.Parent = placed;
        var above = new WorldObjectClickListener(); placed.Parent = above.gameObject;
        var apart = new WorldObjectClickListener();
        Time.unscaledTimeAsDouble = 100;
        Check(InteractableRoles.Read(inside) == InteractableRole.None, "An empty manager names no shrine");
        manager.Objects.Add(placed);
        Check(InteractableRoles.Read(inside) == InteractableRole.None, "Placed shrines are looked up again only at the refresh pace");
        Time.unscaledTimeAsDouble = 106;
        Check(WorldCueIdentityReader.ReadKind(inside) == NaturalCueKind.Shrine && InteractableRoles.Read(above) == InteractableRole.Shrine &&
            InteractableRoles.Read(apart) == InteractableRole.None, "A listener inside or above a placed shrine is a shrine; others are not");
        manager.Objects.WasCollected = true; Time.unscaledTimeAsDouble = 112;
        Check(InteractableRoles.Read(inside) == InteractableRole.None, "A collected shrine list is never read");
        ShrinesManager.Scenes.Clear(); ShrineObjects.Reset();
        var foreign = new WorldObjectClickListener();
        foreach (var (component, expected) in new (Behaviour, NaturalCueKind)[]
        {
            (new TimeRiftInteraction(), NaturalCueKind.Portal), (new ActivateTimeRift(), NaturalCueKind.Portal),
            (new TimeBeastRift(), NaturalCueKind.Portal), (new MonolithPortal(), NaturalCueKind.Portal),
            (new ShatteredRoadPortalInteractable(), NaturalCueKind.Portal),
            (new NemesisEncounterInteractableObject(), NaturalCueKind.Nemesis),
            (new OneShotCacheInteractableObject(), NaturalCueKind.Chest)
        })
        {
            var each = new WorldObjectClickListener();
            component.gameObject = each.gameObject; each.gameObject.Components.Add(component);
            Check(WorldCueIdentityReader.ReadKind(each) == expected, $"{component.GetType().Name} selects {expected}");
            var child = new WorldObjectClickListener(); child.gameObject.Parent = each.gameObject;
            Check(WorldCueIdentityReader.ReadKind(child) == expected, $"{component.GetType().Name} on the owner identifies its listener");
        }
        Check(WorldCueIdentityReader.ReadKind(foreign) == null, "Unrelated listeners stay unclassified");
        var note = new WorldObjectClickListener { Readable = true };
        Check(WorldCueIdentityReader.ReadKind(note) == NaturalCueKind.Tome, "A note, lore record or painting loops the tome");

        // Generated caves: entrance and exits without a hover tag sound like the exits they are.
        var caveEntrance = new WorldObjectClickListener();
        var spawn = (UsePlayerSpawnInteraction)Own(new UsePlayerSpawnInteraction { ToScene = "PCG_Cave" }, caveEntrance);
        Check(UntaggedTransitions.ReadDestination(caveEntrance) == "PCG_Cave" &&
            WorldCueIdentityReader.ReadKind(caveEntrance) == NaturalCueKind.ZoneTransition, "A cave entrance is an exit to the cave");
        spawn.ToScene = " "; spawn.sceneName = "PCG_Cave";
        Check(UntaggedTransitions.ReadDestination(caveEntrance) == "PCG_Cave", "The base transition scene is the fallback");
        spawn.sceneName = null;
        Check(UntaggedTransitions.ReadDestination(caveEntrance) == null && WorldCueIdentityReader.ReadKind(caveEntrance) == null,
            "A transition without a destination is not an exit");
        spawn.ToScene = "PCG_Cave"; spawn.isActiveAndEnabled = false;
        Check(UntaggedTransitions.ReadDestination(caveEntrance) == null, "A disabled transition leads nowhere");
        var lobby = new WorldObjectClickListener();
        lobby.gameObject.scene = new UnityEngine.SceneManagement.Scene(7);
        var lobbyExit = Own(new CaveLobbyExitInteraction(), lobby);
        Check(UntaggedTransitions.ReadDestination(lobby) == null, "A lobby exit outside a generated level leads nowhere");
        UnityEngine.SceneManagement.SceneManager.Names[3] = "B1S30";
        LE.PCG.LevelUtility.Levels[7] = new LE.PCG.Level { ParentScene = new UnityEngine.SceneManagement.Scene(3) };
        Check(UntaggedTransitions.ReadDestination(lobby) == "B1S30" &&
            WorldCueIdentityReader.ReadKind(lobby) == NaturalCueKind.ZoneTransition, "The lobby exit leads back to the cave's parent scene");
        lobbyExit.isActiveAndEnabled = false;
        Check(UntaggedTransitions.ReadDestination(lobby) == null, "A disabled lobby exit leads nowhere");
        LE.PCG.LevelUtility.Levels.Clear();
        SceneList.Scenes["PCG_Cave"] = new SceneDetails { LocalizedName = " Cueva " };
        Check(UntaggedTransitions.SceneName("PCG_Cave") == "Cueva" && UntaggedTransitions.SceneName("B1S30") == null &&
            UntaggedTransitions.SceneName(null) == null, "Destinations take the game's localized scene name");
        SceneList.Scenes.Clear();

        // Names: the game's own text first, the mod catalog only when the game gives none.
        var tag = new HoverTagOnMouseOver { hoverTargetType = HoverTagOnMouseOver.HoverTargetType.OTHER };
        Check(InteractableRoles.Name(InteractableRole.Shrine, tag) == "santuario" &&
            InteractableRoles.Name(InteractableRole.TimeRift, null) == "fisura temporal" &&
            InteractableRoles.Name(InteractableRole.Nemesis, tag) == "Némesis", "Role names fall back to the mod catalog");
        tag.hoverText = "  Altar antiguo ";
        Check(InteractableRoles.Name(InteractableRole.Shrine, tag) == "Altar antiguo", "Authored hover text wins");
        tag.localizationKey = "shrine_key";
        Check(InteractableRoles.Name(InteractableRole.Shrine, tag) == "Altar antiguo", "An unresolved key falls back to the hover text");
        Localization.Texts["shrine_key"] = "Santuario de fuego";
        Check(InteractableRoles.Name(InteractableRole.Shrine, tag) == "Santuario de fuego", "A localization key resolves through the game");
        Localization.Texts.Clear();
        tag.hoverText = null;
        Check(InteractableRoles.Name(InteractableRole.Nemesis, tag) == "Némesis", "An unresolved key without text uses the mod name");
        Check(InteractableRoles.Name(InteractableRole.None, tag) == null, "Without a role or game text there is no name");

        // The echo and Morditas objects: fallback names, travel roles, and the Morditas hover label.
        foreach (var (role, name) in new[] { (InteractableRole.UnstableRift, "fisura inestable"), (InteractableRole.Beacon, "Cristal revelador"),
            (InteractableRole.MonolithForge, "Forja"), (InteractableRole.TimelineCatalyst, "Catalizador de línea temporal"),
            (InteractableRole.OmenMirror, "Espejo de augurio"), (InteractableRole.MorditasCrystal, "Furia de Morditas"),
            (InteractableRole.MorditasReward, "recompensa de Furia de Morditas") })
            Check(InteractableRoles.Name(role, null) == name, $"{role} falls back to its catalog name");
        Check(InteractableRoles.IsTravel(InteractableRole.TimeRift) && InteractableRoles.IsTravel(InteractableRole.UnstableRift) &&
            InteractableRoles.IsTravel(InteractableRole.Beacon) && InteractableRoles.IsTravel(InteractableRole.TimelineCatalyst) &&
            !InteractableRoles.IsTravel(InteractableRole.Nemesis) && !InteractableRoles.IsTravel(InteractableRole.MorditasCrystal) &&
            !InteractableRoles.IsTravel(InteractableRole.OmenMirror) && !InteractableRoles.IsTravel(InteractableRole.None),
            "Rifts, beacons and catalysts are travel roles");
        var crystal = new RageOfMorditasInteractableObject { InteractLabel = new HoverTagOnMouseOver { hoverText = "Cristal de Morditas" } };
        var crystalListener = new WorldObjectClickListener();
        crystal.gameObject = crystalListener.gameObject; crystalListener.gameObject.Components.Add(crystal);
        Check(InteractableRoles.Read(crystalListener) == InteractableRole.MorditasCrystal &&
            InteractableRoles.Name(InteractableRole.MorditasCrystal, crystalListener, null) == "Cristal de Morditas",
            "The Morditas crystal is named by its own hover label");
        crystal.InteractLabel.hoverText = null;
        Check(InteractableRoles.Name(InteractableRole.MorditasCrystal, crystalListener, null) == "Furia de Morditas",
            "A silent Morditas label uses the catalog name");
        var bones = new WorldObjectClickListener();
        var bonePile = new GroundAncientBoneVisuals { gameObject = bones.gameObject }; bones.gameObject.Components.Add(bonePile);
        Check(GroundLootListener.Owns(bones) && !GroundLootListener.Owns(new WorldObjectClickListener()),
            "Ancient bone piles are ground loot, other listeners are not");

        // A shrine's name is found on a disabled tag, above or below the listener; the mod word is the last resort.
        Localization.Texts["shrine_tag"] = "Santuario del viento";
        var shrineListener = new WorldObjectClickListener();
        Check(InteractableRoles.Name(InteractableRole.Shrine, shrineListener, null) == "santuario", "No tag anywhere: the mod word");
        var ownerTag = new HoverTagOnMouseOver { localizationKey = "shrine_tag", isActiveAndEnabled = false };
        ownerTag.gameObject = shrineListener.gameObject; shrineListener.gameObject.Components.Add(ownerTag);
        Check(InteractableRoles.Name(InteractableRole.Shrine, shrineListener, null) == "Santuario del viento",
            "A disabled hover tag still names the shrine");
        var base_ = new WorldObjectClickListener(); var visual = new WorldObjectClickListener();
        visual.gameObject.Parent = base_.gameObject;
        var childTag = new HoverTagOnMouseOver { hoverText = "Altar de luz" };
        childTag.gameObject = visual.gameObject; visual.gameObject.Components.Add(childTag);
        Check(InteractableRoles.Name(InteractableRole.Shrine, base_, null) == "Altar de luz", "A tag on a descendant names the shrine");
        Check(InteractableRoles.Name(InteractableRole.Shrine, visual, null) == "Altar de luz", "A tag on the listener's own object names the shrine");
        Check(InteractableRoles.GameShrineName(new HoverTagOnMouseOver[] { null, new() }) == null, "No game text means no game name");
        Localization.Texts.Clear();
        foreach (var kind in new[] { NaturalCueKind.Shrine, NaturalCueKind.Nemesis })
            Check(WorldCueReach.IsLandmark(kind), $"{kind} is a landmark: it claims a loop voice first and reaches the loop range");
        return checks;
    }
}
