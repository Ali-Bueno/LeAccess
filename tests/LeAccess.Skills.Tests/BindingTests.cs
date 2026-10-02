using LeAccess.Accessibility.Skills;
using Rewired;
using UnityEngine;

internal static class BindingTests
{
    internal static void Reset()
    {
        AbilityBarIcon.all = new NativeArray<AbilityBarIcon>(Array.Empty<AbilityBarIcon>());
        ReInput.isReady = true;
        ReInput.mapping = new Mapping();
        PromptsInfoProvider.Instance = new PromptsInfoProvider();
        PlayerFinder.player = new GameObject();
        var abilities = PlayerFinder.player.Add(new ComboAbilityList { equippedAbilities = new NativeArray<Ability>(new Ability[5]) });
        PlayerFinder.player.Add(new Actor { abilities = abilities });
        AbilityBarSelectorManager.instance = null;
    }

    internal static void Bind(string action, EpochInputManager.InputActions id, params string[] buttons)
    {
        ReInput.mapping.Actions[action] = new InputAction { id = (int)id, name = action };
        PromptsInfoProvider.Instance.Buttons[action] = new NativeArray<ButtonInfo>(
            buttons.Select(button => new ButtonInfo { ButtonName = button }));
    }

    internal static AbilityBarIcon Icon(Ability ability, params string[] actions)
    {
        int slot = AbilityBarIcon.all.Count + 1;
        var icon = new GameObject().Add(new AbilityBarIcon { ability = ability, abilityNumber = slot, player = PlayerFinder.player });
        Equip(slot, ability);
        foreach (string action in actions)
            icon.gameObject.Child().Add(new InputCharacterForAction { actionName = action });
        AbilityBarIcon.all = new NativeArray<AbilityBarIcon>(AbilityBarIcon.all.Concat(new[] { icon }));
        return icon;
    }

    internal static void Equip(int slot, Ability ability)
    {
        var list = (ComboAbilityList)PlayerFinder.getPlayerActor().abilities;
        var entries = list.equippedAbilities.ToArray();
        if (slot < 1 || slot > entries.Length) return;
        entries[slot - 1] = ability;
        list.equippedAbilities = new NativeArray<Ability>(entries);
    }

    internal static void Run(Action<bool, string> check)
    {
        Reset();
        var skill = new Ability { abilityName = "Bola de fuego" };
        var first = Icon(skill, "cast-one");
        Bind("cast-one", EpochInputManager.InputActions.Ability1, " <b>A</b> ");
        check(SkillBindingReader.Read(skill) == "A", "Assigned skill reads its actual localized current button");
        check(PromptsInfoProvider.Instance.LastKeyboardIcons == false,
            "Current device prompt data is requested without forcing keyboard icons");
        Bind("cast-one", EpochInputManager.InputActions.Ability1, "RB", "X");
        check(SkillBindingReader.Read(first) == "RB + X", "All current button labels are read without guessing chord semantics");
        Bind("cast-one", EpochInputManager.InputActions.Ability1, "LT", "Y");
        check(SkillBindingReader.Read(skill) == "LT + Y", "Live remapping is observed without cached slot labels");

        first.gameObject.Child().Add(new InputCharacterForAction { actionName = "cast-one" });
        PromptsInfoProvider.Instance.Queries.Clear();
        check(SkillBindingReader.Read(first) == "LT + Y" && PromptsInfoProvider.Instance.Queries.Count == 1,
            "Duplicate child prompts for one action do not duplicate speech or mapping queries");
        Bind("cast-two", EpochInputManager.InputActions.Ability2, "B");
        Icon(skill, "cast-two");
        Icon(skill, "cast-two");
        check(SkillBindingReader.Read(skill) == "LT + Y, B", "Multiple assigned slots retain distinct buttons and collapse duplicates");
        Bind("overlap", EpochInputManager.InputActions.Ability3, "Y");
        Icon(skill, "overlap");
        check(SkillBindingReader.Read(skill) == "LT + Y, B, Y", "Partially overlapping buttons are retain their actual separate binding groups");

        var unrelated = new Ability { abilityName = "Escudo" };
        Bind("other", EpochInputManager.InputActions.Ability3, "WRONG");
        var nested = first.gameObject.Child().Add(new AbilityBarIcon { ability = unrelated });
        nested.gameObject.Child().Add(new InputCharacterForAction { actionName = "other" });
        check(SkillBindingReader.Read(first) == "LT + Y", "Nested neighboring icon cannot contribute its binding");
        Bind("information", EpochInputManager.InputActions.MoreInformation, "More information");
        first.gameObject.Child().Add(new InputCharacterForAction { actionName = "information" });
        check(SkillBindingReader.Read(first) == "LT + Y", "More information action is not mistaken for a cast binding");
        check(SkillBindingReader.Read(unrelated) is null, "An unregistered or unassigned skill has no invented button");

        Reset();
        var glyphIcon = Icon(skill);
        glyphIcon.gameObject.Child().Add(new InputDependantPrompt { _rewiredActionName = "glyph-cast" });
        glyphIcon.gameObject.Child().Add(new MultiButtonsActionPrompt { _rewiredActionName = "glyph-cast" });
        Bind("glyph-cast", EpochInputManager.InputActions.Ability5, "RB");
        check(SkillBindingReader.Read(skill) == "RB", "Glyph-only typed cast prompts supply binding names without duplicated sources");

        Reset();
        var transformed = new Ability { abilityName = "Forma transformada" };
        var transformedIcon = Icon(transformed, "cast");
        transformedIcon.correspondingAbility = skill;
        Equip(transformedIcon.abilityNumber, skill);
        Bind("cast", EpochInputManager.InputActions.Ability4, "RT");
        check(SkillBindingReader.Read(skill) == "RT", "Corresponding base ability retains its actual transformed slot binding");
        transformedIcon.ability = unrelated;
        transformedIcon.correspondingAbility = unrelated;
        check(SkillBindingReader.Read(skill) == "RT" && SkillBindingReader.Read(unrelated) is null,
            "Changing displayed combo attack cannot change the equipped parent skill's assignment");
        Equip(transformedIcon.abilityNumber, unrelated);
        check(SkillBindingReader.Read(skill) is null && SkillBindingReader.Read(unrelated) == "RT",
            "Actual equipped reassignment immediately changes the binding owner");
        transformedIcon.gameObject.Active = false;
        check(SkillBindingReader.Read(unrelated) == "RT", "Hidden current-player HUD remains an authoritative binding source");
        transformedIcon.player = new GameObject();
        check(SkillBindingReader.Read(transformedIcon) is null, "Prior-scene or other-player HUD cannot claim the current skill binding");

        foreach (string unavailable in new[] { "unready", "mapping", "provider", "buttons", "action", "ability", "equipped", "actor" })
        {
            Reset();
            var candidate = new Ability { abilityName = "Seleccionada" };
            var icon = Icon(candidate, "cast");
            Bind("cast", EpochInputManager.InputActions.Ability5, "LB");
            switch (unavailable)
            {
                case "unready": ReInput.isReady = false; break;
                case "mapping": ReInput.mapping.WasCollected = true; break;
                case "provider": PromptsInfoProvider.Instance.WasCollected = true; break;
                case "buttons": PromptsInfoProvider.Instance.Buttons["cast"].WasCollected = true; break;
                case "action": ReInput.mapping.Actions["cast"].WasCollected = true; break;
                case "equipped": ((ComboAbilityList)PlayerFinder.getPlayerActor().abilities).equippedAbilities.WasCollected = true; break;
                case "actor": PlayerFinder.getPlayerActor().WasCollected = true; break;
                case "ability": candidate.WasCollected = true; break;
            }
            check(SkillBindingReader.Read(candidate) is null, $"Unavailable or collected {unavailable} produces unknown binding safely");
        }
        Reset(); first = Icon(skill, "cast"); Bind("cast", EpochInputManager.InputActions.Ability1, "", " ");
        check(SkillBindingReader.Read(skill) is null, "Empty prompt labels cannot become an assigned-button claim");
        PromptsInfoProvider.Instance.Throw = true;
        check(SkillBindingReader.Read(first) is null, "Prompt-provider failure is contained at the binding boundary");
        Reset();
        first = Icon(skill); Equip(1, null); first.abilityNumber = 4; Equip(4, skill);
        Bind("actual-remapped-slot", EpochInputManager.InputActions.Ability4, "LT", "X");
        check(SkillBindingReader.Read(skill) == "LT + X", "A real glyph-only hotbar icon resolves its authoritative slot without prompt children");
        check(SkillBindingReader.ReadAssignment(skill) == "Equipped: LT + X", "Equipped status explicitly labels the current binding");
        Bind("actual-remapped-slot", EpochInputManager.InputActions.Ability4, "Y");
        check(SkillBindingReader.ReadAssignment(skill) == "Equipped: Y", "Current remapping updates assignment speech");
        first.abilityNumber = 0;
        check(SkillBindingReader.Read(skill) == "Y" && SkillBindingReader.ReadAssignment(skill) == "Equipped: Y", "Equipped list retains assignment independently of stale HUD slot data");
        check(SkillBindingReader.ReadAssignment(unrelated) == "Not equipped", "A valid unmatched ability is explicitly not equipped");
        CheckAuthoritativeAssignment(check);
        CheckPartialAssignmentReadiness(check);
        CheckAssignmentContext(check);
        Reset();
    }

    private static void CheckAuthoritativeAssignment(Action<bool, string> check)
    {
        Reset();
        var flurry = new Ability { abilityName = "Ráfaga" };
        var hit = new Ability { abilityName = "Ráfaga" };
        Equip(2, flurry);
        Bind("actual-slot-two", EpochInputManager.InputActions.Ability2, "Square");
        check(SkillBindingReader.ReadAssignment(flurry) == "Equipped: Square", "Equipped parent resolves even when HUD registry is empty");
        check(SkillBindingReader.ReadAssignment(hit) == "Not equipped", "Equal localized names cannot invent ability identity");
        var bow = new Ability { abilityName = "Ráfaga", overridePrimaryAbility = true, customPrimaryAbility = new AbilityRef { target = flurry } };
        check(SkillBindingReader.ReadAssignment(bow) == "Equipped: Square", "Bow Flurry's declared primary matches equipped melee Flurry");
        Equip(2, bow);
        check(SkillBindingReader.ReadAssignment(flurry) == "Equipped: Square", "Melee Flurry matches its equipped bow variant through declared primary");
        var comboHit = new Ability { abilityName = "Melee attack", overridePrimaryAbility = true, customPrimaryAbility = new AbilityRef { target = flurry } };
        check(SkillBindingReader.ReadAssignment(comboHit) == "Equipped: Square", "Declared Flurry hit primary resolves the same real cast slot");
        var unresolved = new Ability { overridePrimaryAbility = true, customPrimaryAbility = new AbilityRef() };
        check(SkillBindingReader.ReadAssignment(unresolved) is null, "Unresolved requested primary remains unknown instead of falsely not equipped");
        Equip(2, unresolved);
        check(SkillBindingReader.ReadAssignment(flurry) is null, "Unresolved equipped primary prevents false negative assignment");
        Equip(2, flurry);
        var comboList = (ComboAbilityList)PlayerFinder.getPlayerActor().abilities;
        comboList.HasInitialised = false;
        check(SkillBindingReader.ReadAssignment(flurry) is null, "Uninitialized combo list cannot claim a skill is not equipped");
        comboList.HasInitialised = true;
        AbilityBarIcon.all.WasCollected = true;
        check(SkillBindingReader.ReadAssignment(flurry) == "Equipped: Square", "Collected HUD does not erase player equipped state");
        AbilityBarIcon.all = new NativeArray<AbilityBarIcon>(Array.Empty<AbilityBarIcon>());
        var list = (ComboAbilityList)PlayerFinder.getPlayerActor().abilities;
        list.allEquipped = new NativeArray<Ability>(new[] { hit, flurry });
        check(SkillBindingReader.ReadAssignment(hit) == "Not equipped", "Inactive human-form abilities appended by virtual getter do not claim current slots");
        var icon = new GameObject().Add(new AbilityBarIcon { player = PlayerFinder.player, abilityNumber = 2, ability = hit, correspondingAbility = hit });
        check(SkillBindingReader.TryReadSlot(icon, out var current) && current == flurry,
            "Bar identity comes from current equipped list while displayed combo hit differs");
        Equip(2, null);
        check(SkillBindingReader.TryReadSlot(icon, out current) && current is null && SkillBindingReader.ReadAssignment(icon) is null,
            "Empty slot cannot inherit old icon or tooltip assignment");
        icon.abilityNumber = 6;
        check(!SkillBindingReader.TryReadSlot(icon, out _) && SkillBindingReader.ReadSlot(6) is null,
            "Native reserved sixth slot is not an assignable cast slot");
        icon.abilityNumber = -1;
        check(!SkillBindingReader.TryReadSlot(icon, out _), "Invalid slot is rejected without list access");
        list.equippedAbilities.WasCollected = true;
        check(SkillBindingReader.ReadAssignment(flurry) is null,
            "Missing authoritative equipped data means unknown, never not equipped");
        Reset(); Equip(1, flurry); ReInput.isReady = false;
        check(SkillBindingReader.ReadAssignment(flurry) == "Equipped", "Unavailable binding retains proven equipped status");
        Reset();
    }

    private static void CheckPartialAssignmentReadiness(Action<bool, string> check)
    {
        Reset();
        var selected = new Ability { abilityName = "Shurikens" };
        var pending = new Ability { overridePrimaryAbility = true, customPrimaryAbility = new AbilityRef() };
        Equip(1, pending);
        Equip(2, selected);
        Bind("second-slot", EpochInputManager.InputActions.Ability2, "Triangle");
        check(SkillBindingReader.ReadAssignment(selected) == "Equipped: Triangle",
            "First-open unresolved variant in another slot cannot hide a proven equipped skill");
        check(SkillBindingReader.Read(selected) == "Triangle", "Positive slot binding does not depend on unrelated primary loading");
        check(SkillBindingReader.LastStatus == "ready", "Positive assignment is diagnostically ready with another pending slot");
        var absent = new Ability();
        check(SkillBindingReader.ReadAssignment(absent) is null && SkillBindingReader.LastStatus == "primary identity pending",
            "Partial identity snapshot cannot prove a negative assignment");
        var icon = new GameObject().Add(new AbilityBarIcon { player = PlayerFinder.player, abilityNumber = 2 });
        check(SkillBindingReader.TryReadSlot(icon, out var occupant) && occupant == selected,
            "Known slot identity survives another unresolved slot on first hotbar opening");
        check(SkillBindingReader.ReadAssignment(icon) == "Equipped: Triangle", "Selected hotbar slot has its actual assignment immediately");
        icon.abilityNumber = 3;
        check(SkillBindingReader.TryReadSlot(icon, out occupant) && occupant is null,
            "Known empty slot is readable while another slot's primary identity loads");
        Bind("first-slot", EpochInputManager.InputActions.Ability1, "Square");
        check(SkillBindingReader.ReadAssignment(pending) == "Equipped: Square",
            "Exact equipped native object proves its slot even if its optional primary remains unresolved");
        pending.customPrimaryAbility.target = selected;
        check(SkillBindingReader.ReadAssignment(selected) == "Equipped: Square, Triangle",
            "Later primary resolution adds a second proven slot without reinitializing the reader");
        check(SkillBindingReader.ReadAssignment(absent) == "Not equipped", "Resolved complete snapshot can finally establish absence");
        pending.WasCollected = true;
        check(SkillBindingReader.ReadAssignment(selected) == "Equipped: Triangle", "Collected unrelated slot does not erase a valid assignment");
        check(SkillBindingReader.ReadAssignment(absent) is null, "Collected unrelated slot does not become evidence of absence");
        var combo = (ComboAbilityList)PlayerFinder.getPlayerActor().abilities;
        combo.HasInitialised = false;
        check(SkillBindingReader.ReadAssignment(selected) is null && SkillBindingReader.LastStatus == "combo initialization pending",
            "Uninitialized list remains unknown and reports its distinct readiness gate");
        combo.HasInitialised = true;
        check(SkillBindingReader.ReadAssignment(selected) == "Equipped: Triangle", "Same list recovers after initialization without menu reopen");
    }

    private static void CheckAssignmentContext(Action<bool, string> check)
    {
        Reset();
        var ability = new Ability { abilityName = "Ráfaga" };
        var bar = Icon(ability);
        Bind("slot-one", EpochInputManager.InputActions.Ability1, "Square");
        check(SkillAssignmentReader.TryReadContext(bar.gameObject, out var context) &&
            context.Stage == SkillAssignmentStage.Hotbar && context.SlotNumber == 1 && context.Binding == "Square" && context.CurrentAbility == ability,
            "Destination bar slot exposes current ability and actual remapped binding");
        Equip(1, null);
        check(SkillAssignmentReader.TryReadContext(bar.gameObject, out context) && context.CurrentAbility is null && context.Binding == "Square",
            "Empty destination still identifies its real button");
        var grid = new GameObject().Add(new UnityDrivenGridNavigable());
        var icon = grid.gameObject.Child().Add(new AbilitySelectionIcon { ability = ability });
        var manager = new GameObject().Add(new AbilityBarSelectorManager { IsMenuOpened = true, abilityClicked = 1,
            _playerObject = PlayerFinder.player, _gridNavigable = grid });
        AbilityBarSelectorManager.instance = manager;
        check(SkillAssignmentReader.TryReadContext(icon.gameObject, out context) &&
            context.Stage == SkillAssignmentStage.AvailableSkills && context.SlotNumber == 1 && context.Binding == "Square",
            "Available skills expose the manager's clicked destination, including empty slot");
        Equip(1, ability);
        check(SkillAssignmentReader.TryReadContext(icon.gameObject, out context) && context.CurrentAbility == ability,
            "Assignment context refreshes immediately after actual equip");
        manager.abilityClicked = 6;
        check(!SkillAssignmentReader.TryReadContext(icon.gameObject, out _), "Reserved destination is not announced as assignable");
        manager.abilityClicked = 1; manager.IsMenuOpened = false;
        check(!SkillAssignmentReader.TryReadContext(icon.gameObject, out _), "Closed assignment grid cannot provide old context");
        manager.IsMenuOpened = true; manager._playerObject = new GameObject();
        check(!SkillAssignmentReader.TryReadContext(icon.gameObject, out _), "Other-player manager cannot claim assignment context");
        manager._playerObject = PlayerFinder.player; manager._gridNavigable = new GameObject().Add(new UnityDrivenGridNavigable());
        check(!SkillAssignmentReader.TryReadContext(icon.gameObject, out _), "Neighboring grid is not the manager's available skills list");
        manager._gridNavigable = grid; grid.gameObject.Active = false;
        check(!SkillAssignmentReader.TryReadContext(icon.gameObject, out _), "Hidden grid cannot announce stale choices");
        Reset();
    }
}
