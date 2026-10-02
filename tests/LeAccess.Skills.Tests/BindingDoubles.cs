internal sealed class Ability : UnityEngine.Object
{
    internal string abilityName;
    internal bool overridePrimaryAbility;
    internal AbilityRef customPrimaryAbility;
}
internal sealed class AbilityRef : NativeObject
{
    internal Ability target;
    internal bool TryGetAbility(out Ability ability) { Guard(); ability = target; return ability is not null; }
}
internal sealed class AbilityBarIcon : UnityEngine.Component
{
    internal static NativeArray<AbilityBarIcon> all = new(Array.Empty<AbilityBarIcon>());
    internal int abilityNumber;
    internal Ability ability;
    internal Ability correspondingAbility;
    internal AbilityTooltip tooltipAbility { get; set; }
    internal UnityEngine.GameObject player;
}
internal static class PlayerFinder
{
    internal static UnityEngine.GameObject player;
    internal static UnityEngine.GameObject getPlayer() => player;
    internal static Actor getPlayerActor() => player?.GetComponent<Actor>();
}
internal sealed class Actor : UnityEngine.Component
{
    internal AbilityList abilities;
    internal AbilityList GetAbilityList() => abilities;
}
internal class AbilityList : UnityEngine.Component
{
    internal NativeArray<Ability> allEquipped;
    internal NativeArray<Ability> getEquippedAbilities() => allEquipped;
}
internal sealed class ComboAbilityList : AbilityList
{
    internal NativeArray<Ability> equippedAbilities;
    internal bool HasInitialised = true;
}
internal sealed class UnityDrivenGridNavigable : PrioritizedNavigable { }
internal sealed class AbilityBarSelectorManager : UnityEngine.Component
{
    internal static AbilityBarSelectorManager instance;
    internal bool IsMenuOpened;
    internal int abilityClicked;
    internal UnityDrivenGridNavigable _gridNavigable;
    internal UnityEngine.GameObject _playerObject;
}
internal sealed class InputCharacterForAction : UnityEngine.Component { internal string actionName; }
internal sealed class InputDependantPrompt : UnityEngine.Component { internal string _rewiredActionName; }
internal sealed class MultiButtonsActionPrompt : UnityEngine.Component { internal string _rewiredActionName; }
internal static class EpochInputManager
{
    internal enum InputActions { Ability1, Ability2, Ability3, Ability4, Ability5, MoreInformation }
    internal static void SetActiveVirtualMouse(bool value) { }
}
internal sealed class ButtonInfo : NativeObject { internal string ButtonName; }
internal sealed class PromptsInfoProvider : UnityEngine.Object
{
    internal static PromptsInfoProvider Instance;
    internal readonly Dictionary<string, NativeArray<ButtonInfo>> Buttons = new();
    internal readonly List<string> Queries = new();
    internal bool Throw;
    internal bool? LastKeyboardIcons;
    internal NativeArray<ButtonInfo> GetButtonsInfo(string action, bool keyboardIcons)
    {
        Guard();
        if (Throw) throw new InvalidOperationException("Prompt map unavailable");
        Queries.Add(action);
        LastKeyboardIcons = keyboardIcons;
        return Buttons.GetValueOrDefault(action);
    }
}
namespace Rewired
{
    internal sealed class InputAction : NativeObject { internal int id; internal string name; }
    internal sealed class Mapping : NativeObject
    {
        internal readonly Dictionary<string, InputAction> Actions = new();
        internal InputAction GetAction(string name) { Guard(); return Actions.GetValueOrDefault(name); }
        internal InputAction GetAction(int id) { Guard(); return Actions.Values.FirstOrDefault(x => x.id == id); }
    }
    internal static class ReInput
    {
        internal static bool isReady = true;
        internal static Mapping mapping = new();
    }
}

namespace LeAccess.Accessibility.Announcements
{
    internal static class InputBindingText
    {
        internal static string ReadAction(string action, bool useKeyboardIcons = false)
        {
            var buttons = PromptsInfoProvider.Instance.GetButtonsInfo(action, useKeyboardIcons);
            if (buttons is null || buttons.WasCollected) return null;
            return string.Join(" + ", buttons.Where(x => x != null && !x.WasCollected).Select(x => LeAccess.Accessibility.Utils.TextUtils.CleanText(x.ButtonName)).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
        }
    }
}
