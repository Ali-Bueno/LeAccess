using System.Collections;

namespace UnityEngine
{
    internal partial class Object
    {
        private static int _nextId;
        private readonly int _id = ++_nextId;
        public bool WasCollected;
        public bool Destroyed { get; set; }
        public int GetInstanceID() { Guard(); return _id; }
        protected void Guard()
        {
            if (WasCollected) throw new InvalidOperationException("Collected native wrapper accessed");
        }
        public static bool operator ==(Object a, Object b)
        {
            a?.Guard(); b?.Guard();
            return (a is null || a.Destroyed) && (b is null || b.Destroyed) || ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => _id;
    }

    internal class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponentInParent<T>() where T : Component
        {
            for (var root = gameObject; root is not null; root = root.Parent)
            {
                var result = root.Components.OfType<T>().FirstOrDefault();
                if (result is not null) return result;
            }
            return null;
        }
    }

    internal partial class Transform : Component
    {
        public bool IsChildOf(Transform ancestor)
        {
            for (var node = gameObject; node is not null; node = node.Parent)
                if (ReferenceEquals(node.transform, ancestor)) return true;
            return false;
        }
    }

    internal partial class GameObject : Object
    {
        public bool Active = true;
        public bool Visible = true;
        public string name;
        public GameObject Parent;
        public readonly List<Component> Components = new();
        public readonly List<GameObject> Children = new();
        public Transform transform;
        public GameObject() => transform = new Transform { gameObject = this };
        public bool activeInHierarchy => Active && (Parent is null || Parent.activeInHierarchy);
        public T Add<T>(T component) where T : Component
        {
            component.gameObject = this;
            Components.Add(component);
            return component;
        }
        public GameObject Child()
        {
            var child = new GameObject { Parent = this };
            Children.Add(child);
            return child;
        }
        public NativeArray<T> GetComponentsInChildren<T>(bool includeInactive) where T : Component
        {
            var result = new List<T>();
            if (includeInactive || activeInHierarchy)
            {
                result.AddRange(Components.OfType<T>());
                foreach (var child in Children) result.AddRange(child.GetComponentsInChildren<T>(includeInactive));
            }
            return new NativeArray<T>(result);
        }
    }

    internal sealed class CanvasRenderer : Object { public bool cull; public float Alpha = 1; }
    internal struct Color { public float a; }
    internal static class Time { public static int frameCount; }
    internal static class Application { public static bool isFocused = true; }
}

internal sealed class NativeArray<T> : IEnumerable<T>
{
    private readonly List<T> _values;
    public bool WasCollected { get; set; }
    public NativeArray(List<T> values) => _values = values;
    public IEnumerator<T> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

namespace TMPro
{
    internal partial class TMP_Text : UnityEngine.Component
    {
        public string text;
        public bool isActiveAndEnabled = true;
        public float alpha = 1;
        public UnityEngine.CanvasRenderer canvasRenderer = new();
    }
    internal sealed class TMP_InputField : UnityEngine.Component { }
}

namespace UnityEngine.UI
{
    internal sealed class Text : UnityEngine.Component
    {
        public bool isActiveAndEnabled = true;
        public UnityEngine.Color color = new() { a = 1 };
        public string text;
    }
    internal sealed class InputField : UnityEngine.Component { }
}

internal sealed class TutorialManager : UnityEngine.Component
{
    public TutorialNotice _tutorialNotice;
    public UnityEngine.Transform tutorialTooltip { get; set; }
    public TMPro.TMP_Text titleTMP { get; set; }
    public TMPro.TMP_Text bodyTMP { get; set; }
}

internal sealed class TutorialNotice { public bool WasCollected; public string textLocalizationKey; }
internal sealed class MultiButtonsActionPrompt : UnityEngine.Component
{
    public bool isActiveAndEnabled = true, Active = true;
    public string Text;
    public bool IsPromptActive() => Active;
}
internal static class EpochInputManager
{
    public static bool IsControllerActive;
    public enum InputActions { Ability1 = 1, Ability2 = 2, Ability3 = 3, Ability4 = 6, Ability5 = 7,
        UsePotion = 8, UsePortal = 9, Inventory = 11, SkillMenu = 13, PasiveGrid = 15, CraftingMenu = 16,
        QuestLog = 41, ControllerMenuToggle = 61, SocialPanel = 70, LootFilter = 76, DodgeRoll = 192 }
}
namespace Rewired
{
    internal static class ReInput { public static bool isReady = true; public static Mapping mapping = new(); }
    internal sealed class Mapping
    {
        public bool WasCollected;
        public readonly Dictionary<int, InputAction> Actions = new();
        public InputAction GetAction(int id) => Actions.GetValueOrDefault(id);
    }
    internal sealed class InputAction { public bool WasCollected; public string name; }
}

namespace LE.Factions
{
    internal sealed class BinaryChoiceModalWindow : UnityEngine.Component
    {
        public TMPro.TMP_Text headerText { get; set; }
        public TMPro.TMP_Text contentText { get; set; }
    }
}
namespace LE.UI.Generic.UnityUI
{
    internal sealed class ConfirmationPanel : UnityEngine.Component
    {
        public TMPro.TMP_Text HeaderText { get; set; }
        public TMPro.TMP_Text BodyText { get; set; }
    }
}
namespace LE.UI { internal static class LoadingScreen { public static bool IsVisible { get; set; } } }
namespace LE.Tos
{
    internal sealed partial class TosPanel : UnityEngine.Component
    {
        public string Title;
        public TosContent _displayedContent;
    }
    internal sealed class TosContent { public bool WasCollected; public string Content; }
}

namespace LeAccess.Accessibility.Announcements
{
    internal static class InputBindingText
    {
        public static readonly Dictionary<string, string> Bindings = new();
        public static string ReadAction(string action) => Bindings.GetValueOrDefault(action);
        public static string ReadPrompt(MultiButtonsActionPrompt prompt) =>
            UiVisibility.IsVisible(prompt.gameObject) ? prompt.Text : null;
    }
    internal static class UiVisibility
    {
        public static int PresentationChecks;
        public static bool HasVisiblePresentation(UnityEngine.GameObject root)
        {
            PresentationChecks++;
            if (!IsVisible(root)) return false;
            var labels = root.GetComponentsInChildren<TMPro.TMP_Text>(true).Cast<UnityEngine.Component>()
                .Concat(root.GetComponentsInChildren<UnityEngine.UI.Text>(true)).ToList();
            return labels.Count == 0 || labels.Any(IsVisibleGraphic);
        }
        public static bool IsVisibleGraphic(UnityEngine.Component label) => label is not null && !label.WasCollected &&
            IsVisible(label.gameObject) && (label switch
            {
                TMPro.TMP_Text text => text.isActiveAndEnabled && float.IsFinite(text.alpha) && text.alpha > 0 &&
                    !text.canvasRenderer.cull && float.IsFinite(text.canvasRenderer.Alpha) && text.canvasRenderer.Alpha > 0,
                UnityEngine.UI.Text text => text.isActiveAndEnabled && float.IsFinite(text.color.a) && text.color.a > 0,
                _ => false
            });
        public static bool IsVisible(UnityEngine.GameObject root)
        {
            for (var node = root; node is not null; node = node.Parent)
                if (node.WasCollected || !node.Active || !node.Visible) return false;
            return true;
        }
    }
}
namespace LeAccess.Accessibility.Utils
{
    internal static class TextUtils
    {
        public static string CleanText(string text) => text?.Trim();
        public static string GetTextFromHierarchy(UnityEngine.GameObject go) => go.GetComponentsInChildren<TMPro.TMP_Text>(false)
            .Where(IsVisibleLabel).Select(label => CleanText(label.text)).FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
        public static bool IsVisibleLabel(TMPro.TMP_Text label) =>
            LeAccess.Accessibility.Announcements.UiVisibility.IsVisibleGraphic(label);
    }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        public static bool SpokeThisFrame;
        public static bool OutputAccepted = true;
        public static readonly List<(string Text, bool Interrupt)> Messages = new();
        public static bool IsAvailable() => true;
        public static void Speak(string text, bool interrupt) => TrySpeak(text, interrupt);
        public static bool TrySpeak(string text, bool interrupt = true)
        {
            if (!OutputAccepted) return false;
            Messages.Add((text, interrupt));
            return true;
        }
    }
}
namespace LeAccess
{
    internal static class Plugin { public static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
    }
}
