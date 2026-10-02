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
    }

    internal static void Bind(string action, EpochInputManager.InputActions id, params string[] buttons)
    {
        ReInput.mapping.Actions[action] = new InputAction { id = (int)id, name = action };
        PromptsInfoProvider.Instance.Buttons[action] = new NativeArray<ButtonInfo>(
            buttons.Select(button => new ButtonInfo { ButtonName = button }));
    }

    internal static AbilityBarIcon Icon(Ability ability, params string[] actions)
    {
        var icon = new GameObject().Add(new AbilityBarIcon { ability = ability, player = PlayerFinder.player });
        foreach (string action in actions)
            icon.gameObject.Child().Add(new InputCharacterForAction { actionName = action });
        AbilityBarIcon.all = new NativeArray<AbilityBarIcon>(AbilityBarIcon.all.Concat(new[] { icon }));
        return icon;
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
        Bind("cast", EpochInputManager.InputActions.Ability4, "RT");
        check(SkillBindingReader.Read(skill) == "RT", "Corresponding base ability retains its actual transformed slot binding");
        transformedIcon.ability = unrelated;
        transformedIcon.correspondingAbility = unrelated;
        check(SkillBindingReader.Read(skill) is null && SkillBindingReader.Read(unrelated) == "RT",
            "Reassigned slot stops attributing its button to the previous skill");
        transformedIcon.gameObject.Active = false;
        check(SkillBindingReader.Read(unrelated) == "RT", "Hidden current-player HUD remains an authoritative binding source");
        transformedIcon.player = new GameObject();
        check(SkillBindingReader.Read(unrelated) is null, "Prior-scene or other-player HUD cannot claim the current skill binding");

        foreach (string unavailable in new[] { "unready", "mapping", "provider", "buttons", "action", "icon", "registry", "ability" })
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
                case "icon": icon.WasCollected = true; break;
                case "registry": AbilityBarIcon.all.WasCollected = true; break;
                case "ability": candidate.WasCollected = true; break;
            }
            check(SkillBindingReader.Read(candidate) is null, $"Unavailable or collected {unavailable} produces unknown binding safely");
        }
        Reset(); first = Icon(skill, "cast"); Bind("cast", EpochInputManager.InputActions.Ability1, "", " ");
        check(SkillBindingReader.Read(skill) is null, "Empty prompt labels cannot become an assigned-button claim");
        PromptsInfoProvider.Instance.Throw = true;
        check(SkillBindingReader.Read(first) is null, "Prompt-provider failure is contained at the binding boundary");
        Reset();
        first = Icon(skill); first.abilityNumber = 4;
        Bind("actual-remapped-slot", EpochInputManager.InputActions.Ability4, "LT", "X");
        check(SkillBindingReader.Read(skill) == "LT + X", "A real glyph-only hotbar icon resolves its authoritative slot without prompt children");
        check(SkillBindingReader.ReadAssignment(skill) == "Equipped: LT + X", "Equipped status explicitly labels the current binding");
        Bind("actual-remapped-slot", EpochInputManager.InputActions.Ability4, "Y");
        check(SkillBindingReader.ReadAssignment(skill) == "Equipped: Y", "Current remapping updates assignment speech");
        first.abilityNumber = 0;
        check(SkillBindingReader.Read(skill) is null && SkillBindingReader.ReadAssignment(skill) == "Equipped", "Unknown slot does not invent a button or lose actual equipped status");
        check(SkillBindingReader.ReadAssignment(unrelated) == "Not equipped", "A valid unmatched ability is explicitly not equipped");
        Reset();
    }
}
