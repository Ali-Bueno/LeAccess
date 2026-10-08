using LeAccess.Accessibility;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class CreationChoiceTests
{
    internal static int Run(Action reset)
    {
        int checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
        GameObject Named(GameObject parent, string name) { var go = parent.Child(); go.name = name; return go; }
        Button Control(GameObject parent, string name, string label)
        {
            var go = Named(parent, name); var button = go.Add(new Button());
            Named(go, "Text (TMP)").Add(new TextMeshProUGUI { text = label });
            return button;
        }
        reset(); ModText.SetLanguage("es");
        var root = new GameObject(); var slots = root.Add(new LocalCharacterSlots()); LocalCharacterSlots.instance = slots;
        var character = root.Add(new CharacterSelect()); CharacterSelect.instance = character;
        var challenges = Named(root, "Challenges");
        var account = Control(challenges, "Solo Account Found", "Botín en solitario - cuenta");
        var solo = Control(challenges, "Solo Character Found", "Botín en solitario - personaje");
        slots.soloChallengeCheckmark = Named(account.gameObject, "selected");
        slots.soloCharacterChallengeCheckmark = Named(solo.gameObject, "selected");
        var modes = Named(root, "Game Modes");
        var standard = Control(modes, "Standard", "Normal"); var hardcore = Control(modes, "Hardcore", "Difícil");
        var cycleBanner = Named(root, "Cycle-Realm-Selected"); var legacyBanner = Named(root, "Legacy-Realm-Selected");
        character.CharacterCreationRealm_Cycle = cycleBanner; character.CharacterCreationRealm_Legacy = legacyBanner;
        var cycleChange = Control(cycleBanner, "changeRealmButton", "Cambiar de reino");
        var legacyChange = Control(legacyBanner, "changeRealmButton", "Cambiar de reino");
        var cycleTitle = Named(cycleBanner, "CycleName").Add(new TextMeshProUGUI { text = "Temporada 5" });
        Named(legacyBanner, "Name").Add(new TextMeshProUGUI { text = "Legado" });
        string text;

        Check(CreationChoiceReader.TryRead(solo.gameObject, out text) && text == "Botín en solitario - personaje, casilla, no marcada",
            "unchecked character challenge");
        slots.isSoloCharacterChallenge = true;
        Check(CreationChoiceReader.TryRead(solo.gameObject.Child(), out text) && text.EndsWith("casilla, marcada"),
            "child of the control resolves; checked reads the game flag");
        Check(CreationChoiceReader.TryRead(account.gameObject, out text) && text == "Botín en solitario - cuenta, casilla, no marcada",
            "account challenge reads its own flag, not the character's");
        Check(CreationChoiceReader.TryRead(standard.gameObject, out text) && text == "Normal, seleccionado", "standard selected by default");
        Check(CreationChoiceReader.TryRead(hardcore.gameObject, out text) && text == "Difícil, no seleccionado", "hardcore not selected by default");
        slots.isHardcore = true;
        Check(CreationChoiceReader.TryRead(hardcore.gameObject, out text) && text == "Difícil, seleccionado", "hardcore follows isHardcore");
        Check(CreationChoiceReader.TryRead(standard.gameObject, out text) && text == "Normal, no seleccionado", "standard deselected by hardcore");
        Check(CreationChoiceReader.TryRead(cycleChange.gameObject, out text) && text == "Cambiar de reino, reino actual: Temporada 5", "cycle realm title");
        Check(CreationChoiceReader.TryRead(legacyChange.gameObject, out text) && text == "Cambiar de reino, reino actual: Legado", "legacy realm title");
        cycleTitle.text = "";
        Check(CreationChoiceReader.TryRead(cycleChange.gameObject, out text) && text == "Cambiar de reino", "missing realm title speaks only the label");
        cycleTitle.text = "Temporada 5";

        ModText.SetLanguage("en");
        Check(CreationChoiceReader.TryRead(account.gameObject, out text) && text == "Botín en solitario - cuenta, checkbox, not checked", "English keys");
        ModText.SetLanguage("es");

        var other = Control(challenges, "Other", "Otra");
        Check(!CreationChoiceReader.TryRead(other.gameObject, out _), "unrelated control");
        Check(!CreationChoiceReader.TryRead(new GameObject(), out _), "object without a button");
        var stray = Control(root, "Hardcore", "Difícil");
        Check(!CreationChoiceReader.TryRead(stray.gameObject, out _), "Hardcore outside Game Modes");
        var strayRealm = Control(root, "changeRealmButton", "Cambiar");
        Check(!CreationChoiceReader.TryRead(strayRealm.gameObject, out _), "realm button outside the typed banners");
        var ambiguous = Control(challenges, "Ambiguous", "Uno"); Named(ambiguous.gameObject, "Second").Add(new TextMeshProUGUI { text = "Dos" });
        Check(!CreationChoiceReader.TryRead(ambiguous.gameObject, out _), "ambiguous label");
        var foreignButton = Control(new GameObject(), "Solo Account Found", "Botín");
        Check(!CreationChoiceReader.TryRead(foreignButton.gameObject, out _), "control outside LocalCharacterSlots");
        slots.soloChallengeCheckmark = null;
        Check(!CreationChoiceReader.TryRead(account.gameObject, out _), "missing checkmark reference");
        slots.soloChallengeCheckmark = Named(account.gameObject, "selected");
        account.gameObject.Active = false;
        Check(!CreationChoiceReader.TryRead(account.gameObject, out _), "inactive control");
        account.gameObject.Active = true;
        LocalCharacterSlots.instance = null;
        Check(!CreationChoiceReader.TryRead(solo.gameObject, out _), "missing slots instance");
        slots.WasCollected = true; LocalCharacterSlots.instance = slots;
        Check(!CreationChoiceReader.TryRead(solo.gameObject, out _), "collected slots instance");
        slots.WasCollected = false;

        // State changes are spoken once, only while the control is focused.
        LocalCharacterSlots.instance = slots; ScreenReader.Messages.Clear(); CreationChoiceReader.Reset();
        slots.isSoloCharacterChallenge = false; slots.isHardcore = false; slots.isSoloChallenge = false;
        bool focused = true; Func<GameObject, bool> isFocused = _ => focused;
        CreationChoiceReader.TryRead(solo.gameObject, out _);
        CreationChoiceReader.Poll(isFocused);
        Check(ScreenReader.Messages.Count == 0, "no speech without a change");
        slots.isSoloCharacterChallenge = true;
        focused = false; CreationChoiceReader.Poll(isFocused);
        Check(ScreenReader.Messages.Count == 0, "unfocused change stays silent");
        focused = true; CreationChoiceReader.Poll(isFocused); CreationChoiceReader.Poll(isFocused);
        Check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0] == ("marcada", true), "checked state spoken once");
        slots.isSoloCharacterChallenge = false; CreationChoiceReader.Poll(isFocused);
        Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages[1].Text == "no marcada", "unchecked state spoken");
        CreationChoiceReader.TryRead(hardcore.gameObject, out _);
        slots.isHardcore = true; CreationChoiceReader.Poll(isFocused);
        Check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages[2].Text == "Difícil, seleccionado", "mode change speaks the new mode");
        // Pressing the selected Normal also flips to hardcore: the change names Difícil.
        slots.isHardcore = false; CreationChoiceReader.TryRead(standard.gameObject, out _);
        slots.isHardcore = true; CreationChoiceReader.Poll(isFocused);
        Check(ScreenReader.Messages.Count == 4 && ScreenReader.Messages[3].Text == "Difícil, seleccionado", "pressing the selected mode names the new one");
        CreationChoiceReader.TryRead(cycleChange.gameObject, out _);
        cycleTitle.text = "Temporada 6"; CreationChoiceReader.Poll(isFocused);
        Check(ScreenReader.Messages.Count == 5 && ScreenReader.Messages[4].Text.EndsWith("Temporada 6"), "realm change speaks the new realm");
        CreationChoiceReader.TryRead(account.gameObject, out _);
        slots.isSoloChallenge = true; account.gameObject.Active = false; CreationChoiceReader.Poll(isFocused);
        account.gameObject.Active = true; CreationChoiceReader.Poll(isFocused);
        Check(ScreenReader.Messages.Count == 5, "a hidden control drops tracking instead of speaking");
        slots.isSoloChallenge = false;
        CreationChoiceReader.TryRead(account.gameObject, out _);
        slots.isSoloChallenge = true; CreationChoiceReader.TryRead(account.gameObject, out _); CreationChoiceReader.Poll(isFocused);
        Check(ScreenReader.Messages.Count == 5, "a focus read after the change absorbs it");
        reset();
        return checks;
    }
}
