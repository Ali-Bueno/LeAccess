using UnityEngine;
using TMPro;
using UnityEngine.UI;
namespace Il2CppSystem.Collections.Generic
{
    internal class List<T> : NativeObject, System.Collections.Generic.IEnumerable<T>
    {
        private readonly System.Collections.Generic.List<T> _values = new();
        internal List(params T[] items) => _values.AddRange(items);
        internal int Count { get { Guard(); return _values.Count; } }
        internal T this[int index] { get { Guard(); return _values[index]; } set { Guard(); _values[index] = value; } }
        public IEnumerator<T> GetEnumerator() { Guard(); return _values.GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
internal class PrioritizedNavigable : Component { internal Selectable ActiveSelectable; }
internal sealed class ListNavigable : PrioritizedNavigable
{ internal Il2CppSystem.Collections.Generic.List<Selectable> _selectableChildren; internal int _lastSelectedIndex; }
internal static class NavigationManager { internal static NativeObject currentlySelected; }
internal enum CharacterClassID { Primalist, Mage, Sentinel, Acolyte, Rogue }
internal class CharacterClass : UnityEngine.Object { internal string className; internal CharacterClassID classID; }
internal class CharacterCreationSelection : Selectable
{
    internal enum CharacterContext { NewCharacter, ExistingCharacter }
    internal CharacterContext characterContext;
    internal CharacterClass characterClass;
}
internal class CharacterSelect : Component
{
    internal enum CharacterSelectState { ClassSelection, NameCreation, CharacterSelection }
    internal CharacterSelectState currentState;
    internal static CharacterSelect instance;
}
internal class MasteryButton : Component
{
    internal TextMeshProUGUI levelTMP;
    internal GameObject masteredTMP;
    internal bool isMasteryClass;
    internal string classDescription, classDescriptionLocalizationKey;
    internal Il2CppSystem.Collections.Generic.List<string> passiveBonuses, bonusDescriptionLocalizationKey;
}
internal class MasteryButtonTooltip : Component { internal string masteryName; }
internal class MultiButtonsActionPrompt : Component { }
internal class InputDependantPrompt : Component { }
internal class InputCharacterForAction : Component { }
internal class MasteryPanel : Component
{ internal NativeArray<Selectable> masteryButtons; internal NativeArray<TextMeshProUGUI> masteryButtonTMPs; }
internal class MasteryBanner : Component
{ internal CharacterClassID characterClassID; internal Il2CppSystem.Collections.Generic.List<MasteryButton> masteryButtons; }
internal class MasteryTabButton : Component { internal TabButton _tabButton; }
internal class TabButton : Component { internal int SelectedIndex; }
internal class TabUIElement : Selectable
{ internal TabUIController controller; internal int elementID; }
internal class TabUIController : Component
{ internal Il2CppSystem.Collections.Generic.List<GameObject> tabContents; }
namespace UnityEngine.Localization.Components
{
    internal class LocalizeStringEvent : Component { internal LocalizedString StringReference; }
    internal class LocalizedString : NativeObject { internal EntryReference TableEntryReference; }
    internal class EntryReference : NativeObject { internal string Key; }
}
internal class PassivePanelData
{
    internal class MasteryData : NativeObject
    {
        internal string name, descriptionLocalizationKey, description;
        internal bool isMastery;
        internal Il2CppSystem.Collections.Generic.List<string> bonusLocalizationKeys, bonusFallbackText;
    }
}
namespace LE.UI.PanelSystem
{
    internal class FullScreenPanelPassiveTree : Component
    {
        internal Il2CppSystem.Collections.Generic.List<MasteryTabButton> _masteryTabButtons;
        internal Il2CppSystem.Collections.Generic.List<PassivePanelData.MasteryData> _masteryDataList;
        internal int _currentMasteryIndex;
    }
}
internal static class Localization
{
    internal static readonly Dictionary<string, string> Texts = new();
    internal static CommonLocalization Common = new();
    internal static string GetText(string key, string fallback) => Texts.TryGetValue(key, out var value) ? value : fallback;
}
internal class CommonLocalization : NativeObject
{
    internal string GetMastery(string value) => Localization.GetText("mastery:" + value, value);
    internal string GetClass(string value) => Localization.GetText("class:" + value, value);
}
namespace LeAccess.Accessibility.ControllerMenus
{ internal static class ControllerMenuAccessibility { internal static bool HasActiveContent; } }
