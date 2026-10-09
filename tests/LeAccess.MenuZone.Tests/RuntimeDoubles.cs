internal class NativeObject
{
    public bool WasCollected { get; set; }
    public T TryCast<T>() where T : class => this as T;
}
namespace UnityEngine
{
    internal class Object : NativeObject
    {
        private static int _nextId;
        private readonly int _id = ++_nextId;
        public int GetInstanceID() => _id;
        public static bool operator ==(Object a, Object b)
        {
            if (a?.WasCollected == true || b?.WasCollected == true)
                throw new InvalidOperationException("Collected Unity object comparison");
            return ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => _id;
    }
    internal class Component : Object
    {
        public GameObject gameObject = new();
        public bool isActiveAndEnabled = true;
    }
    internal sealed class GameObject : Object
    {
        public bool activeInHierarchy = true;
        public bool Visible = true;
        public bool HasPresentation = true;
        public string Text;
    }
    internal sealed class CanvasRenderer : Object { public bool cull { get; set; } }
    internal static class Application { public static bool isFocused = true; }
}
namespace UnityEngine.SceneManagement
{
    internal struct Scene { public int handle; public string name; public bool isLoaded; }
    internal static class SceneManager
    {
        public static Scene Current = new() { handle = 1, name = "camp", isLoaded = true };
        public static Scene GetActiveScene() => Current;
    }
}
namespace TMPro
{
    internal sealed class TMP_Text : UnityEngine.Component
    {
        public string text;
        public float alpha = 1;
        public UnityEngine.CanvasRenderer canvasRenderer = new();
    }
}
internal sealed class ControllerManager : UnityEngine.Component
{
    public static ControllerManager instance;
    public NativeObject _CurrentMap_k__BackingField;
}
internal sealed class ControllerMenuMap : NativeObject { public ControllerMenu controllerMenu; }
internal sealed class ControllerMenu : UnityEngine.Component
{
    public bool _enabled = true;
    public RadialMenu FocusedRadialMenu;
}
internal sealed class RadialMenu : UnityEngine.Component
{
    public sealed class OptionData : NativeObject { public string m_TextKey; }
    public bool _enabled = true;
    public RadialMenuSector _selectedSector;
}
internal sealed class RadialMenuSector : UnityEngine.Component
{
    public bool _selected = true;
    public TMPro.TMP_Text _optionText = new();
    public RadialMenu.OptionData _assignedOption;
}
internal static class Localization
{
    public static readonly Dictionary<string, string> Texts = new() { ["menu.inventory"] = "Inventario" };
    public static string GetText(string key, string fallback = "") => Texts.TryGetValue(key, out var text) ? text : fallback;
}
internal sealed class SceneDetails : NativeObject { public string LocalizedName; public SceneList.Era era = SceneList.Era.EndOfTime; }
internal static class SceneList
{
    public enum Era { Ancient, Divine, Imperial, Ruined, EndOfTime }
    public static readonly Dictionary<string, SceneDetails> Known = new();
    public static bool TryGetNonDefaultSceneDetails(string scene, out SceneDetails details) => Known.TryGetValue(scene, out details);
}
internal static class PlayerFinder { public static UnityEngine.GameObject player; }
internal sealed class LocationNotificationManager : UnityEngine.Component
{
    public bool showLocation = true;
    public UnityEngine.GameObject holder = new();
    public TMPro.TMP_Text nameText = new();
}
namespace LE.UI { internal static class LoadingScreen { public static bool IsVisible; } }
namespace LeAccess.Accessibility.Announcements
{
    internal static class UiVisibility
    {
        public static bool HasVisiblePresentation(UnityEngine.GameObject root) =>
            IsVisible(root) && root.HasPresentation;
        public static bool IsVisible(UnityEngine.GameObject root) =>
            root is not null && !root.WasCollected && root.activeInHierarchy && root.Visible;
    }
}
namespace LeAccess.Accessibility.ScreenContent
{
    internal static class ScreenContentAnnouncer { public static bool HasActiveContent; }
    internal static class ScreenContentReader
    {
        public static string Read(UnityEngine.GameObject root, TMPro.TMP_Text header, TMPro.TMP_Text body)
        {
            string title = header is not null && !header.WasCollected && header.isActiveAndEnabled && header.alpha > 0
                ? header.text : null;
            return ScreenContentTracker.Combine(new[] { title, body?.text, root.Text });
        }
    }
}
namespace LeAccess.Accessibility.Dialogue { internal static class DialogueAnnouncer { public static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationAudioState { public static bool IsSuspended; }
    internal static class GameplayInputState
    {
        public static bool Gameplay = true;
        public static bool OwnsInput() => Gameplay;
    }
}
namespace LeAccess.Accessibility.Utils
{
    internal static class TextUtils
    {
        public static string CleanText(string text) => text?.Trim();
        public static bool IsVisibleLabel(TMPro.TMP_Text label) =>
            label is not null && !label.WasCollected && label.isActiveAndEnabled &&
            float.IsFinite(label.alpha) && label.alpha > 0 &&
            LeAccess.Accessibility.Announcements.UiVisibility.IsVisible(label.gameObject) &&
            (label.canvasRenderer is null || !label.canvasRenderer.cull);
    }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        public static readonly List<(string Text, bool Interrupt)> Messages = new();
        public static bool Available = true;
        public static bool SpokeThisFrame;
        public static bool IsAvailable() => Available;
        public static void Speak(string text, bool interrupt) { Messages.Add((text, interrupt)); SpokeThisFrame = true; }
    }
}
namespace LeAccess
{
    internal static class Plugin { public static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        public readonly List<string> Warnings = new();
        public void LogInfo(string message) { }
        public void LogWarning(string message) => Warnings.Add(message);
    }
}
