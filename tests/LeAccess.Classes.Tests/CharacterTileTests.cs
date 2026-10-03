using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.CharacterSheet;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;

internal static class CharacterTileTests
{
    internal static int Run(Action reset)
    {
        int checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
        reset();
        var root = new GameObject();
        var tile = root.Add(new CharacterTile());
        tile.charName = root.Child().Add(new TextMeshProUGUI { text = "Ali" });
        tile.charLvlAndMastery = root.Child().Add(new TextMeshProUGUI { text = "Nivel 40 Paladín" });
        tile.charCycle = root.Child().Add(new TextMeshProUGUI { text = "Temporada" });
        tile.hardcoreTag = root.Child(); tile.hardcoreTag.Add(new TextMeshProUGUI { text = "Hardcore" });
        var button = root.Child();
        Check(CharacterTileReader.TryRead(button, out string text) && text == "Ali. Nivel 40 Paladín. Temporada. Hardcore",
            "tile reads name, level and mastery, cycle and active hardcore tag in order");
        tile.hardcoreTag.Active = false;
        Check(CharacterTileReader.TryRead(root, out text) && text == "Ali. Nivel 40 Paladín. Temporada", "inactive hardcore tag is not spoken");
        Check(!CharacterTileReader.TryRead(new GameObject(), out _), "non-tile control is not read as a character tile");
        Check(TextUtils.ReadableObjectName("Button") == null && TextUtils.ReadableObjectName("Toggle(Clone)") == null &&
            TextUtils.ReadableObjectName("Image (1)") == null && TextUtils.ReadableObjectName("BtnButton") == null,
            "bare role words are never spoken");
        Check(TextUtils.ReadableObjectName("CloseButton") == "Close" && TextUtils.ReadableObjectName("Confirm Btn") == "Confirm" &&
            TextUtils.ReadableObjectName("BackButton(Clone)") == "Back", "role words are stripped from icon-only names");
        Check(TextUtils.ReadableObjectName("SpecialOfferPanel") == "Special Offer Panel", "specific object names remain usable");

        var statRoot = new GameObject();
        var line = statRoot.Add(new CharacterStatDisplayLineItem());
        statRoot.Child().Add(new TextMeshProUGUI { text = "Armadura" });
        line._statValueDisplay = statRoot.Child().Add(new TextMeshProUGUI { text = "1250" });
        var focused = statRoot.Child();
        Check(CharacterStatLineReader.TryRead(focused, out text) && text == "Armadura. 1250", "stat line reads its name then its typed value");
        line._statValueDisplay.text = "";
        Check(!CharacterStatLineReader.TryRead(focused, out _), "a stat line without a value falls back to the generic reader");
        Check(!CharacterStatLineReader.TryRead(new GameObject(), out _), "non-stat controls are not read as stat lines");
        return checks;
    }
}
