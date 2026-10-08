namespace UnityEngine
{
    internal class Object
    {
        private static int _next;
        internal readonly int Id = ++_next;
        internal IntPtr Pointer => (IntPtr)GetInstanceID();
        internal bool WasCollected, Destroyed;
        internal int GetInstanceID() => WasCollected ? throw new Exception("Collected native wrapper accessed") : Id;
        public static bool operator ==(Object a, Object b) => (a is null || a.Destroyed) && (b is null || b.Destroyed) || ReferenceEquals(a,b);
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this,other);
        public override int GetHashCode() => Id;
    }
    internal class Component : Object { internal GameObject gameObject; internal Transform transform => gameObject.transform; }
    internal class Behaviour : Component { internal bool Enabled = true; internal bool isActiveAndEnabled => Enabled && gameObject.activeInHierarchy; }
    internal sealed class Transform : Component
    {
        internal bool IsChildOf(Transform owner)
        { for (var node = gameObject; node is not null; node = node.Parent) if (node.transform == owner) return true; return false; }
    }
    internal sealed class GameObject : Object
    {
        internal bool Active = true, Visible = true;
        internal GameObject Parent;
        internal Transform transform;
        internal readonly List<Component> Components = new();
        internal readonly List<GameObject> Children = new();
        internal SceneManagement.Scene scene = SceneManagement.SceneManager.Active;
        internal GameObject() => transform = new() { gameObject = this };
        internal bool activeInHierarchy => Active && (Parent is null || Parent.activeInHierarchy);
        internal T Add<T>(T value) where T : Component { value.gameObject = this; Components.Add(value); return value; }
        internal GameObject Child() { var child = new GameObject { Parent = this }; Children.Add(child); return child; }
    }
    internal static class Time { internal static double unscaledTimeAsDouble; }
    internal static class Application { internal static bool isFocused = true; }
}
namespace UnityEngine.SceneManagement
{
    internal struct Scene { internal int handle; internal string name; internal bool isLoaded; }
    internal static class SceneManager
    { internal static Scene Active = new() { handle = 1, name = "Echo", isLoaded = true }; internal static Scene GetActiveScene() => Active; }
}
namespace TMPro
{
    internal sealed class TMP_Text : UnityEngine.Behaviour
    { internal string text; internal float alpha = 1; internal bool Culled; }
}
internal sealed class NativeList<T> : List<T> { internal bool WasCollected; }
internal sealed class QuestTrackerUI : UnityEngine.Behaviour
{
    internal static QuestTrackerUI instance;
    internal bool initialised = true;
    internal string currentScene = "Echo";
    internal UnityEngine.GameObject questHolder;
    internal NativeList<QuestTrackerQuestUI> questsDisplayed = new();
}
internal sealed class QuestTrackerQuestUI : UnityEngine.Behaviour
{
    internal object quest;
    internal bool TracksMonolithObjective, TracksMonolithModifiers;
    internal TMPro.TMP_Text questName, chapterText;
    internal UnityEngine.GameObject objectiveObject;
    internal NativeList<QuestTrackerObjectiveUI> objectivesDisplayed = new();
}
internal sealed class QuestTrackerObjectiveUI : UnityEngine.Behaviour { internal TMPro.TMP_Text text; }
internal sealed class RageOfMorditasQuestTrackerUI : UnityEngine.Behaviour
{
    internal static RageOfMorditasQuestTrackerUI Instance;
    internal UnityEngine.SceneManagement.Scene _encounterScene;
    internal QuestTrackerQuestUI questHeader;
    internal QuestTrackerObjectiveUI _currentObjective;
    internal NativeList<QuestTrackerObjectiveUI> _activeObjectives = new();
}
internal static class PlayerFinder
{
    internal static UnityEngine.GameObject player = new();
    internal static UnityEngine.Object Character = new();
    internal static Provider Provider = new();
    internal static UnityEngine.Object getPlayerData() => Character;
    internal static Provider getLocalStatefulQuestListProvider() => Provider;
}
internal static class SceneList
{ internal static bool Gameplay = true; internal static bool TryGetNonDefaultSceneDetails(string name, out object details) { details = new(); return Gameplay; } }
internal sealed class EpochInputManager : UnityEngine.Object
{ internal static EpochInputManager instance = new(); internal bool isInputFieldActive; }
internal sealed class ScreenKeyboardManager : UnityEngine.Object
{ internal static ScreenKeyboardManager Instance = new(); internal bool _enabled; }
namespace LE.UI { internal static class LoadingScreen { internal static bool IsVisible; } }
namespace LeAccess.Accessibility.Navigation
{
    internal static class GameplayInputState { internal static bool Gameplay = true; internal static bool OwnsInput() => Gameplay; }
    internal static class NavigationAudioState { internal static bool IsSuspended; }
}
namespace LeAccess.Accessibility.Announcements
{
    internal static class UiVisibility
    {
        internal static bool IsVisible(UnityEngine.GameObject root)
        { for (var node = root; node is not null; node = node.Parent) if (node.WasCollected || node.Destroyed || !node.Active || !node.Visible) return false; return root is not null; }
        internal static bool HasVisiblePresentation(UnityEngine.GameObject root) => IsVisible(root);
    }
    internal static class UiReadingOrder
    {
        internal readonly record struct Fragment(UnityEngine.Component Component, string Text);
        internal static IEnumerable<string> Read(IEnumerable<Fragment> fragments) => fragments.Select(f => f.Text);
    }
}
namespace LeAccess.Accessibility.Utils
{
    internal static class TextUtils
    {
        internal static string CleanText(string text) => text?.Trim();
        internal static bool IsVisibleLabel(TMPro.TMP_Text text) => text is not null && !text.WasCollected && !text.Destroyed &&
            text.isActiveAndEnabled && !text.Culled && text.alpha > 0 && LeAccess.Accessibility.Announcements.UiVisibility.IsVisible(text.gameObject);
    }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        internal static bool Available = true, SpokeThisFrame;
        internal static bool OutputAccepted = true;
        internal static readonly List<(string Text, bool Interrupt)> Messages = new();
        internal static bool IsAvailable() => Available;
        internal static void Speak(string text, bool interrupt) => TrySpeak(text, interrupt);
        internal static bool TrySpeak(string text, bool interrupt)
        {
            if (!Available || !OutputAccepted) return false;
            Messages.Add((text,interrupt)); SpokeThisFrame = true; return true;
        }
    }
}
namespace LeAccess
{
    internal static class Plugin { internal static readonly Logger Log = new(); }
    internal sealed class Logger
    { internal readonly List<string> Warnings = new(); internal void LogInfo(string text) { } internal void LogWarning(string text) => Warnings.Add(text); }
}
namespace LeAccess.Accessibility.Players
{
    internal static class LocalPlayerSource
    {
        internal static UnityEngine.Object Online;
        internal static UnityEngine.GameObject Player => PlayerFinder.player;
        internal static UnityEngine.Object OnlineLocal() => Online;
    }
}
