using LeAccess.Accessibility.CharacterCreation;
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
        Check(TextUtils.ReadableObjectName("Close Button") == null && TextUtils.ReadableObjectName("Toggle(Clone)") == null,
            "generic icon-only object names are never spoken");
        Check(TextUtils.ReadableObjectName("SpecialOfferPanel") == "Special Offer Panel", "specific object names remain usable");
        return checks;
    }
}
