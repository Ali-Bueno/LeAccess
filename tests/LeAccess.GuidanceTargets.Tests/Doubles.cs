namespace UnityEngine.SceneManagement
{
    internal struct Scene
    {
        internal int handle;
        internal bool isLoaded, Valid;
        internal bool IsValid() => Valid;
    }
}
namespace UnityEngine
{
    internal class Object
    {
        private static int _next;
        internal bool WasCollected, Destroyed;
        internal readonly IntPtr Pointer = new(++_next);
        internal int GetInstanceID() => Pointer.ToInt32();
        internal T TryCast<T>() where T : class => this as T;
        public static bool operator ==(Object a, Object b) =>
            (a is null || a.Destroyed) && (b is null || b.Destroyed) || ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => Pointer.ToInt32();
    }
    internal class GameObject : Object
    {
        internal bool activeInHierarchy = true;
        internal SceneManagement.Scene scene = new() { handle = 1, isLoaded = true, Valid = true };
        internal Transform transform;
        internal readonly List<Component> Components = new();
        internal GameObject() => transform = new Transform { gameObject = this };
        internal T GetComponent<T>() where T : Component => Components.OfType<T>().FirstOrDefault();
        internal T Add<T>() where T : Component, new()
        {
            var component = new T { gameObject = this };
            Components.Add(component);
            return component;
        }
    }
    internal class Component : Object
    {
        internal GameObject gameObject;
        internal Transform transform => gameObject.transform;
        internal T GetComponent<T>() where T : Component => gameObject.Components.OfType<T>().FirstOrDefault();
        internal List<T> GetComponentsInParent<T>(bool inactive) where T : Component
        {
            var result = new List<T>();
            for (var current = transform; current != null; current = current.parent)
                if (inactive || current.gameObject.activeInHierarchy) result.AddRange(current.gameObject.Components.OfType<T>());
            return result;
        }
        internal T GetComponentInParent<T>(bool inactive) where T : Component => GetComponentsInParent<T>(inactive).FirstOrDefault();
    }
    internal class Transform : Component
    {
        internal Transform parent; internal Vector3 position;
        internal bool IsChildOf(Transform ancestor)
        {
            for (var current = this; current != null; current = current.parent) if (current == ancestor) return true;
            return false;
        }
    }
    internal class RectTransform : Transform
    {
        internal Rect Bounds = new(0, 0, 100, 100);
        internal void GetWorldCorners(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> corners)
        {
            corners[0] = new(Bounds.x, Bounds.y, 0); corners[1] = new(Bounds.x, Bounds.y + Bounds.height, 0);
            corners[2] = new(Bounds.x + Bounds.width, Bounds.y + Bounds.height, 0); corners[3] = new(Bounds.x + Bounds.width, Bounds.y, 0);
        }
    }
    internal class Behaviour : Component
    {
        internal bool enabled = true;
        internal bool isActiveAndEnabled => enabled && gameObject.activeInHierarchy;
    }
    internal class Canvas : Behaviour { }
    internal class MeshRenderer : Component { internal bool enabled = true, forceRenderingOff; }
    internal class CanvasGroup : Behaviour { internal float alpha = 1; internal bool ignoreParentGroups; }
    internal class Sprite : Object { }
    internal class CanvasRenderer : Object { internal bool cull; internal float Alpha = 1; internal float GetAlpha() => Alpha; }
    internal struct Color { internal float a; }
    internal readonly record struct Vector3(float x, float y, float z);
    internal readonly record struct Rect(float x, float y, float width, float height)
    {
        internal bool Overlaps(Rect other) => x < other.x + other.width && x + width > other.x &&
            y < other.y + other.height && y + height > other.y;
    }
}
namespace UnityEngine.UI
{
    internal class Image : UnityEngine.Behaviour
    {
        internal UnityEngine.Color color = new() { a = 1 };
        internal UnityEngine.Canvas canvas;
        internal UnityEngine.CanvasRenderer canvasRenderer = new();
        internal UnityEngine.Sprite overrideSprite = new();
        internal UnityEngine.RectTransform rectTransform => transform as UnityEngine.RectTransform;
    }
}
internal class NativeList<T> : List<T> { internal bool WasCollected; }
namespace DMM
{
    internal class BaseDMMapIcon : UnityEngine.Behaviour { }
    internal class DMMapIcon : BaseDMMapIcon { }
}
namespace LE.UI.Minimap
{
    internal class Minimap : UnityEngine.Behaviour { }
    internal class MinimapIconUI : UnityEngine.Behaviour { internal UnityEngine.UI.Image MainImage, ArrowImage; }
    internal class MinimapIconManager : UnityEngine.Behaviour
    {
        internal static MinimapIconManager Instance;
        internal Minimap minimap;
        internal NativeList<IconInfo> icons = new();
        internal class IconInfo
        {
            internal bool WasCollected;
            internal DMM.DMMapIcon icon;
            internal MinimapIconUI ui;
        }
    }
}
namespace LE.Data { internal static class SavedQuest { internal enum TrackStatus { AUTO_TRACKED, AUTO_UNTRACKED, FORCE_TRACKED, FORCE_UNTRACKED } } }
internal enum QuestState { NotStarted, Active, Completed, Failed, NoLongerRelevant }
internal class Quest : UnityEngine.Object
{
    internal string displayName = "The Missing Letter";
    internal int id = 1;
    internal bool playerVisible = true, mainLineQuest;
    internal bool ThrowOnRead;
    internal StatefulQuestList List;
    internal StatefulQuestList getLocalStatefulQuestList() => ThrowOnRead ? throw new InvalidOperationException("unavailable native quest") : List;
}
internal class QuestStep : UnityEngine.Object { internal int id = 2; internal Quest quest; }
internal class Objective : UnityEngine.Object { internal Quest quest; internal QuestStep questStep; internal int id = 3; internal string activeText = "Find the letter"; }
internal class ObjectiveCondition : UnityEngine.Behaviour { internal Objective objective; }
internal class SetMapIconColourFromObjectiveCondition : UnityEngine.Behaviour
{
    internal ObjectiveCondition objectiveCondition;
    internal DMM.DMMapIcon icon, arrowIcon;
}
internal class StatefulQuestList
{
    internal bool WasCollected, Tracked = true;
    internal QuestState ObjectiveState = QuestState.Active;
    internal StatefulQuest State;
    internal bool UIIsQuestTracked(Quest _) => Tracked;
    internal QuestState getObjectiveState(Objective _) => ObjectiveState;
    internal StatefulQuest getStatefulQuest(Quest _) => State;
}
internal class StatefulQuest
{
    internal bool WasCollected;
    internal Quest quest;
    internal LE.Data.SavedQuest.TrackStatus trackStatus;
    internal QuestState State = QuestState.Active;
    internal QuestState getState() => State;
}
internal class DMMapWorldIcon : UnityEngine.Behaviour { internal DMM.BaseDMMapIcon MmIcon; internal bool awaitingDestruction; }
internal class WorldObjectClickListener : UnityEngine.Behaviour
{
    internal HoverTagOnMouseOver hoverTagOnMouseOver;
    internal float interactionRange = 2;
    internal bool Interactable = true;
    internal bool CanInteract() => Interactable;
}
internal class HoverTagOnMouseOver : UnityEngine.Behaviour
{
    internal enum HoverTargetType { ZONE_TRANSITION, DUNGEON_TRANSITION, SAME_SCENE_TRANSITION, OTHER }
    internal HoverTargetType hoverTargetType;
    internal TransitionOrGateInteraction sceneInteraction, dungeonTransitionInteraction;
}
internal class TransitionOrGateInteraction : UnityEngine.Behaviour { internal string sceneName; }
internal class SceneDetails { internal string LocalizedName; internal bool WasCollected; }
internal static class SceneList
{
    internal static readonly Dictionary<string, SceneDetails> Scenes = new();
    internal static bool TryGetNonDefaultSceneDetails(string name, out SceneDetails details) => Scenes.TryGetValue(name, out details);
}
internal static class Localization { internal static QuestLocalization Quests; }
internal class QuestLocalization
{
    internal bool WasCollected;
    internal string GetQuestName(int _, string fallback) => "Localized " + fallback;
    internal string GetObjectiveActive(int _, int __, int ___, string fallback) => "Localized " + fallback;
}
namespace LeAccess.Accessibility.MapSections
{
    internal readonly record struct MapSectionWorldSnapshot(int SceneHandle) { internal long Session { get; init; } }
}
namespace LeAccess.Accessibility.Utils { internal static class TextUtils { internal static string CleanText(string text) => text?.Trim(); } }
namespace LeAccess.I18n
{
    internal static class ModText
    {
        internal static string Get(string key) => key;
        internal static string Format(string key, params object[] args) => key switch
        {
            "guidance.quest_marker" => $"{args[0]}. Objective marker: {args[1]}",
            "guidance.exit_to" => $"Exit to {args[0]}",
            _ => throw new Exception("Unknown key " + key)
        };
    }
}
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
    internal sealed class Il2CppStructArray<T> : List<T>
    {
        internal Il2CppStructArray(int count) : base(Enumerable.Repeat(default(T), count)) { }
    }
}
